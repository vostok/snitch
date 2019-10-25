using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Hercules.Consumers;
using Vostok.Logging.Abstractions;
using Vostok.Metrics.Grouping;
using Vostok.Metrics.Primitives.Gauge;
using Vostok.Metrics.Primitives.Timer;
using Vostok.Snitch.Grouping;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch
{
    [PublicAPI]
    public class Snitch
    {
        private readonly SnitchSettings settings;
        private readonly ILog log;
        private readonly Dictionary<Target, WindowsOfTarget> targets;
        private readonly StreamReader<HerculesHttpSpan> streamReader;
        
        private readonly IMetricGroup1<IIntegerGauge> eventsMetric;
        private readonly IMetricGroup1<IIntegerGauge> stateMetric;
        private readonly IMetricGroup1<ITimer> iterationMetric;

        private volatile StreamShardingSettings shardingSettings;
        private volatile StreamCoordinates leftCoordinates;
        private volatile StreamCoordinates rightCoordinates;

        private volatile bool restart;

        public Snitch(SnitchSettings settings, ILog log)
        {
            this.settings = settings;
            this.log = log;

            var streamReaderSettings = new StreamReaderSettings<HerculesHttpSpan>(
                settings.StreamName,
                settings.StreamClient)
            {
                EventsReadBatchSize = settings.EventsReadBatchSize,
                EventsReadTimeout = settings.EventsReadTimeout
            };

            streamReader = new StreamReader<HerculesHttpSpan>(streamReaderSettings, log);
            targets = new Dictionary<Target, WindowsOfTarget>();
            
            eventsMetric = settings.MetricContext.CreateIntegerGauge("events", "type", new IntegerGaugeConfig { ResetOnScrape = true });
            stateMetric = settings.MetricContext.CreateIntegerGauge("state", "type");
            settings.MetricContext.CreateFuncGauge("events", "type").For("remaining").SetValueProvider(CountStreamRemainingEvents);
            iterationMetric = settings.MetricContext.CreateSummary("iteration", "type", new SummaryConfig { Quantiles = new[] { 0.5, 0.75, 1 } });
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // (iloktionov): Catch-up with state for other shards on any change to our sharding settings:
                    var newShardingSettings = settings.ShardingSettingsProvider();
                    if (shardingSettings == null || !shardingSettings.Equals(newShardingSettings))
                    {
                        log.Info(
                            "Observed new sharding settings: shard with index {ShardIndex} from {ShardCount}. Restarting snitch.",
                            newShardingSettings.ClientShardIndex,
                            newShardingSettings.ClientShardCount);

                        shardingSettings = newShardingSettings;

                        restart = true;
                    }

                    if (restart)
                    {
                        await Restart(cancellationToken).ConfigureAwait(false);
                        restart = false;
                    }

                    using (iterationMetric.For("time").Measure())
                    {
                        await MakeIteration(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception error)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    log.Error(error);

                    await Task.Delay(settings.DelayOnError, cancellationToken).SilentlyContinue().ConfigureAwait(false);
                }
            }
        }

        private async Task Restart(CancellationToken cancellationToken)
        {
            //leftCoordinates = await settings.LeftCoordinatesStorage.GetCurrentAsync().ConfigureAwait(false);
            //rightCoordinates = await settings.RightCoordinatesStorage.GetCurrentAsync().ConfigureAwait(false);

            leftCoordinates = await streamReader.SeekToEndAsync(shardingSettings, cancellationToken).ConfigureAwait(false);
            rightCoordinates = leftCoordinates;

            targets.Clear();

            log.Info("Updated coordinates from storage: left: {LeftCoordinates}, right: {RightCoordinates}.", leftCoordinates, rightCoordinates);

            var segmentReaderSettings = new StreamSegmentReaderSettings<HerculesHttpSpan>(
                settings.StreamName,
                settings.StreamClient,
                leftCoordinates,
                rightCoordinates)
            {
                EventsReadBatchSize = settings.EventsReadBatchSize,
                EventsReadTimeout = settings.EventsReadTimeout
            };

            var segmentReader = new StreamSegmentReader<HerculesHttpSpan>(segmentReaderSettings, log);

            var coordinates = leftCoordinates;

            while (true)
            {
                var (query, result) = await segmentReader.ReadAsync(coordinates, shardingSettings, cancellationToken).ConfigureAwait(false);
                if (result == null)
                    break;

                foreach (var @event in result.Payload.Events)
                {
                    var target = new Target(@event.TargetEnvironment, @event.TargetService);
                    if (!targets.ContainsKey(target))
                        targets[target] = new WindowsOfTarget(target, settings, log);
                    targets[target].AddSpan(@event, query.Coordinates);
                }

                foreach (var target in targets)
                {
                    target.Value.Process(true);
                }

                coordinates = result.Payload.Next;
            }

            rightCoordinates = coordinates;
            log.Info("Coordinates after restart: left: {LeftCoordinates}, right: {RightCoordinates}.", leftCoordinates, rightCoordinates);
        }

        private async Task MakeIteration(CancellationToken cancellationToken)
        {
            var (query, readResult) = await streamReader.ReadAsync(rightCoordinates, shardingSettings, cancellationToken).ConfigureAwait(false);

            var eventsDropped = 0;

            foreach (var @event in readResult.Payload.Events)
            {
                var target = new Target(@event.TargetEnvironment, @event.TargetService);
                if (!targets.ContainsKey(target))
                    targets[target] = new WindowsOfTarget(target, settings, log);
                if (!targets[target].AddSpan(@event, query.Coordinates))
                    eventsDropped++;
            }

            var result = new ProcessResult();
            var staleTargets = new List<Target>();

            foreach (var target in targets)
            {
                var processResult = target.Value.Process();
                result.AddProcessResult(processResult);

                if (processResult.ActiveEventsCount == 0
                    && DateTime.UtcNow - target.Value.LastEventAdded > settings.Ttl)
                    staleTargets.Add(target.Key);
            }

            foreach (var target in staleTargets)
            {
                targets.Remove(target);
            }

            leftCoordinates = result.FirstActiveEventCoordinates ?? readResult.Payload.Next;
            rightCoordinates = readResult.Payload.Next;

            //await SaveProgress().ConfigureAwait(false);

            LogProgress(result, readResult.Payload.Events.Count, eventsDropped);

            if (readResult.Payload.Events.Count == 0)
            {
                await Task.Delay(settings.DelayOnNoEvents, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SaveProgress()
        {
            try
            {
                await settings.RightCoordinatesStorage.AdvanceAsync(rightCoordinates).ConfigureAwait(false);
                await settings.LeftCoordinatesStorage.AdvanceAsync(leftCoordinates).ConfigureAwait(false);
                log.Info("Saved coordinates: left: {LeftCoordinates}, right: {RightCoordinates}.", leftCoordinates, rightCoordinates);
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to save coordinates: left: {LeftCoordinates}, right: {RightCoordinates}.", leftCoordinates, rightCoordinates);
            }
        }

        private void LogProgress(ProcessResult result, int eventsIn, int eventsDropped)
        {
            log.Info(
                "Global snitch progress: events in: {EventsIn}, events dropped: {EventsDropped}.",
                eventsIn,
                eventsDropped);

            iterationMetric.For("in").Report(eventsIn);
            eventsMetric.For("in").Add(eventsIn);
            eventsMetric.For("dropped").Add(eventsDropped);

            log.Info(
                "Global snitch status: targets: {TargetsCount}, windows: {WindowsCount}, events: {EventsCount}.",
                targets.Count,
                result.ActiveWindowsCount,
                result.ActiveEventsCount);

            stateMetric.For("targets").Set(targets.Count);
            stateMetric.For("windows").Set(result.ActiveWindowsCount);
            stateMetric.For("events").Set(result.ActiveEventsCount);
        }

        private double CountStreamRemainingEvents()
        {
            var remaining = streamReader.CountStreamRemainingEventsAsync(rightCoordinates, shardingSettings).GetAwaiter().GetResult();
            log.Info("Global snitch progress: stream remaining events: {EventsRemaining}.", remaining);
            return remaining;
        }
    }
}