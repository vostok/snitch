using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Configuration;
using Vostok.Datacenters;
using Vostok.Hercules.Consumers;
using Vostok.Logging.Abstractions;
using Vostok.Metrics;
using Vostok.Metrics.Models;
using Vostok.Snitch.AggregatedEvents;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics.Models;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Metrics
{
    public class MetricsProcessor
    {
        private readonly TopologyKey topology;
        private readonly MetricsProcessorSettings settings;

        private readonly Dictionary<MetricTags, IMetric> metrics;
        private readonly MetricsProcessorServiceSettings serviceSettings;

        public MetricsProcessor(TopologyKey topology, MetricsProcessorSettings settings)
        {
            this.topology = topology;
            this.settings = settings;

            serviceSettings = settings.For(topology.Service);

            metrics = new Dictionary<MetricTags, IMetric>();
        }

        public void Add(AggregatedEvent @event)
        {
            GetOrCreateMetric(@event.Key, @event.Type == AggregatedEventType.Counter).Add(@event);
        }

        public void Add(HerculesHttpClientSpan span)
        {
            AddCommon(span);
            AddClient(span);
        }

        public void Add(HerculesHttpClusterSpan span)
        {
            AddCommon(span);
            AddCluster(span);
        }

        public void Write(IMetricContext metricContext, TopologyStatisticsCollector statisticsCollector, ILog log, DateTimeOffset timestamp)
        {
            LogRps(log, timestamp);

            try
            {
                WriteAvailabilityStatistics(statisticsCollector, timestamp);

                if (SelectReplicas().Any())
                    WriteReplicasStatistics(statisticsCollector, timestamp);
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to write aggregated metrics for service '{Service}' at environment '{Environment}'.", topology.Service, topology.Environment);
            }

            var sent = 0;
            var topClients = SelectTopClients();
            var topUrls = SelectTopUrls();

            var service = topology.GetMetricName();
            metricContext = metricContext.WithTag(WellKnownTagKeys.Name, service);

            foreach (var (metricTags, metricValue) in metrics)
            {
                if (ShouldBeFiltered(metricTags, topClients, topUrls))
                    continue;

                var context = metricContext.WithTags(metricTags);

                foreach (var point in metricValue.ToMetricDataPoints())
                {
                    point.Timestamp = timestamp;
                    context.Send(point);
                    sent++;
                }
            }

            LogSent(log, timestamp, sent);
        }

        public void Write(StreamBinaryEventsWriter eventsWriter, ILog log, DateTimeOffset timestamp)
        {
            LogRps(log, timestamp);

            foreach (var (metricTags, metricValue) in metrics)
            {
                var aggregated = metricValue.ToAggregatedEventValues();

                var @event = new AggregatedEvent
                {
                    TargetEnvironment = topology.Environment,
                    TargetService = topology.Service,
                    Key = metricTags,
                    Values = aggregated,
                    Timestamp = timestamp,
                    Type = metricValue is Histogram ? AggregatedEventType.Histogram : AggregatedEventType.Counter
                };

                eventsWriter.Put(b => AggregatedEventWriter.Build(@event, b));
            }
        }

        private void AddClient(HerculesHttpClientSpan span)
        {
            var responseCode = span.ResponseCode.ToString("D");
            var topologyReplica = span.GetTopologyReplica();
            var replica = topologyReplica.ToString();
            var datacenterFrom = DatacentersProvider.Get().GetDatacenter(span.Host);
            var datacenterTo = DatacentersProvider.Get().GetDatacenter(span.RequestUrl.Host);
            var latency = span.Latency.TotalMilliseconds;
            var requestSize = span.RequestSize ?? 0;
            var responseSize = span.ResponseSize ?? 0;
            var responseCodeClass = settings.ResponseCodeClassifier.Classify(span.TargetService, span.ResponseCode, span.Latency);
            var availabilityResponseCodeClass = settings.AvailabilityResponseCodeClassifier.Classify(span.TargetService, span.ResponseCode, span.Latency);

            AddCounter(MetricsTags.RpsPerReplica.For(replica), 1);
            AddCounter(MetricsTags.RpsPerUnescapedReplica.For(replica), 1);
            AddCounter(MetricsTags.RpsPerResponseCodeAndReplica.For(responseCode, replica), 1);
            AddTimer(MetricsTags.LatencyPerReplica.For(replica), latency);

            switch (responseCodeClass)
            {
                case ResponseCodeClass.Success:
                    AddCounter(MetricsTags.InstantSuccessResponses.For(), 1, false);
                    AddCounter(MetricsTags.InstantReplicaSuccessResponses.For(replica), 1, false);
                    AddCounter(MetricsTags.InstantSuccessLatency.For(), latency, false);
                    AddCounter(MetricsTags.InstantReplicaSuccessLatency.For(replica), latency, false);
                    AddCounter(MetricsTags.InstantSuccessSquaredLatency.For(), latency * latency, false);
                    AddCounter(MetricsTags.InstantReplicaSuccessSquaredLatency.For(replica), latency * latency, false);
                    break;
                case ResponseCodeClass.Warning:
                    AddCounter(MetricsTags.InstantWarningResponses.For(), 1, false);
                    AddCounter(MetricsTags.InstantReplicaWarningResponses.For(replica), 1, false);
                    AddCounter(MetricsTags.InstantWarningLatency.For(), latency, false);
                    AddCounter(MetricsTags.InstantReplicaWarningLatency.For(replica), latency, false);
                    AddCounter(MetricsTags.InstantWarningSquaredLatency.For(), latency * latency, false);
                    AddCounter(MetricsTags.InstantReplicaWarningSquaredLatency.For(replica), latency * latency, false);
                    break;
                case ResponseCodeClass.Error:
                    AddCounter(MetricsTags.InstantErrorResponses.For(), 1, false);
                    AddCounter(MetricsTags.InstantReplicaErrorResponses.For(replica), 1, false);
                    AddCounter(MetricsTags.InstantErrorLatency.For(), latency, false);
                    AddCounter(MetricsTags.InstantReplicaErrorLatency.For(replica), latency, false);
                    AddCounter(MetricsTags.InstantErrorSquaredLatency.For(), latency * latency, false);
                    AddCounter(MetricsTags.InstantReplicaErrorSquaredLatency.For(replica), latency * latency, false);
                    break;
            }

            switch (availabilityResponseCodeClass)
            {
                case ResponseCodeClass.Success:
                    AddCounter(MetricsTags.AvailabilitySuccessResponses.For(), 1, false);
                    break;
                case ResponseCodeClass.Warning:
                    AddCounter(MetricsTags.AvailabilityWarningResponses.For(), 1, false);
                    break;
                case ResponseCodeClass.Error:
                    AddCounter(MetricsTags.AvailabilityErrorResponses.For(), 1, false);
                    break;
            }

            AddCounter(MetricsTags.RequestTrafficPerDatacenterLink.For(datacenterFrom, datacenterTo), requestSize);
            AddCounter(MetricsTags.ResponseTrafficPerDatacenterLink.For(datacenterFrom, datacenterTo), responseSize);
            AddCounter(MetricsTags.RpsPerDatacenterLink.For(datacenterFrom, datacenterTo), 1);
        }

        private void AddCluster(HerculesHttpClusterSpan span)
        {
            var status = span.Status;
            var strategy = span.Strategy;
            var client = span.Application;
            var url = span.FormatUrl();
            var responseCodeClass = settings.AvailabilityResponseCodeClassifier.Classify(span.TargetService, span.ResponseCode, span.Latency);

            AddCounter(MetricsTags.RpsPerStatus.For(status), 1);
            AddCounter(MetricsTags.RpsPerStrategy.For(strategy), 1);

            AddCounter(MetricsTags.RpsPerStatusAndClient.For(status, client), 1);
            AddCounter(MetricsTags.RpsPerResponseCodeAndUrl.For(status, url), 1);

            switch (responseCodeClass)
            {
                case ResponseCodeClass.Success:
                    AddCounter(MetricsTags.InstantSuccessResponses.For(), 1, false);
                    AddCounter(MetricsTags.AvailabilitySuccessResponses.For(), 1, false);
                    break;
                case ResponseCodeClass.Warning:
                    AddCounter(MetricsTags.InstantWarningResponses.For(), 1, false);
                    AddCounter(MetricsTags.AvailabilityWarningResponses.For(), 1, false);
                    break;
                case ResponseCodeClass.Error:
                    AddCounter(MetricsTags.InstantErrorResponses.For(), 1, false);
                    AddCounter(MetricsTags.AvailabilityErrorResponses.For(), 1, false);
                    break;
            }
        }

        private void AddCommon(HerculesHttpSpan span)
        {
            var responseCode = span.ResponseCode.ToString("D");
            var client = span.Application;
            var url = span.FormatUrl();
            var requestSize = span.RequestSize ?? 0;
            var responseSize = span.ResponseSize ?? 0;
            var requestSizeBucket = MetricsHelper.GetSizeBucket(requestSize, serviceSettings.RequestSizeBuckets);
            var responseSizeBucket = MetricsHelper.GetSizeBucket(responseSize, serviceSettings.ResponseSizeBuckets);
            var latency = span.Latency.TotalMilliseconds;

            AddCounter(MetricsTags.Rps.For(), 1);
            AddCounter(MetricsTags.RpsPerResponseCode.For(responseCode), 1);
            AddCounter(MetricsTags.RpsPerUrl.For(url), 1);
            AddCounter(MetricsTags.RpsPerClient.For(client), 1);
            AddCounter(MetricsTags.RpsPerResponseCodeAndClient.For(responseCode, client), 1);
            AddCounter(MetricsTags.RpsPerResponseCodeAndUrl.For(responseCode, url), 1);
            AddCounter(MetricsTags.RpsPerClientAndUrl.For(client, url), 1);

            AddTimer(MetricsTags.Latency.For(), latency);
            AddTimer(MetricsTags.LatencyPerClient.For(client), latency);
            AddTimer(MetricsTags.LatencyPerUrl.For(url), latency);

            if (requestSizeBucket != null)
                AddTimer(MetricsTags.LatencyPerUrlAndRequestSize.For(url, requestSizeBucket), latency);
            if (responseSizeBucket != null)
                AddTimer(MetricsTags.LatencyPerUrlAndResponseSize.For(url, responseSizeBucket), latency);

            AddTimer(MetricsTags.RequestSize.For(), requestSize);
            AddTimer(MetricsTags.ResponseSize.For(), responseSize);
            AddTimer(MetricsTags.RequestSizePerClient.For(client), requestSize);
            AddTimer(MetricsTags.ResponseSizePerClient.For(client), responseSize);
            AddTimer(MetricsTags.RequestSizePerUrl.For(url), requestSize);
            AddTimer(MetricsTags.ResponseSizePerUrl.For(url), responseSize);
        }

        private void WriteAvailabilityStatistics(TopologyStatisticsCollector statisticsCollector, DateTimeOffset timestamp)
        {
            var availabilityStatistics = BuildAvailabilityStatistics(timestamp);
            statisticsCollector.Put(availabilityStatistics);

            var instantGlobalStatistic = new ReplicaStatistics(
                timestamp,
                GetCount(MetricsTags.InstantSuccessResponses.For()),
                GetCount(MetricsTags.InstantWarningResponses.For()),
                GetCount(MetricsTags.InstantErrorResponses.For()),
                0,
                0,
                0,
                0,
                0,
                0);

            metrics.Add(MetricsTags.InstantSuccessFraction.For(), new Final(instantGlobalStatistic.SuccessFraction));
            metrics.Add(MetricsTags.InstantWarningFraction.For(), new Final(instantGlobalStatistic.WarningFraction));
            metrics.Add(MetricsTags.InstantErrorFraction.For(), new Final(instantGlobalStatistic.ErrorFraction));
        }

        private void WriteReplicasStatistics(TopologyStatisticsCollector statisticsCollector, DateTimeOffset timestamp)
        {
            var instantStatistics = BuildInstantStatistics(timestamp);

            var warningPenalty = serviceSettings.PenaltyMultiplierForWarnings * instantStatistics.Global.LatencyStdDeviation;
            var errorPenalty = serviceSettings.PenaltyMultiplierForErrors * instantStatistics.Global.LatencyStdDeviation;
            var penalizedStatistic = instantStatistics.PenalizeWarning(warningPenalty).PenalizeError(errorPenalty);

            var smoothingMetricsConstant = serviceSettings.MetricsSmoothingTimeConstant;
            var previousStatistics = settings.History.GetPreviousStatistics(topology);
            var smoothedStatistic = penalizedStatistic.SmoothAggregates(previousStatistics, smoothingMetricsConstant);

            // TODO(kungurtsev): remove this.
            var smoothedStatistic2 = new Statistics
            {
                Global = smoothedStatistic.Global,
                Replicas = smoothedStatistic.Replicas
            };
            smoothedStatistic.Global = instantStatistics.Global.SmoothAggregates(previousStatistics?.Global, smoothingMetricsConstant);

            var accumulateWith = settings.History.GetPreviousStatistics(topology, ConfigurationProvider.Default.Get<MetricsSettings>().AccumulateLength);
            var accumulatedStatistic = smoothedStatistic.UnionWith(accumulateWith);

            var weightByStatuses = MetricsHelper.Scale(smoothedStatistic.Global.TotalCount)
                                   <= smoothedStatistic.Replicas.Count * serviceSettings.WeightByStatusesRpsThreshold;
            var instantWeights = weightByStatuses
                ? BuildInstantWeightsByStatuses(timestamp, smoothedStatistic)
                : BuildInstantWeightsByLatencies(timestamp, smoothedStatistic);
            var instantWeights2 = BuildInstantWeightsByLatencies2(timestamp, smoothedStatistic2);
            var previousWeights = settings.History.GetPreviousWeights(topology);
            var smoothingWeightsConstant = serviceSettings.WeightsSmoothingTimeConstant;
            var smoothedWeights = instantWeights.Smooth(previousWeights, smoothingWeightsConstant);
            smoothedWeights.NormalizeAndEnforceLimits(serviceSettings.MinimumWeight);

            metrics.Add(MetricsTags.InstantLatencyMean.For(), new Final(instantStatistics.Global.LatencyMean));
            metrics.Add(MetricsTags.InstantLatencyStdDeviation.For(), new Final(instantStatistics.Global.LatencyStdDeviation));
            foreach (var (replica, replicaStatistics) in instantStatistics.Replicas)
            {
                metrics.Add(MetricsTags.InstantReplicaLatencyMean.For(replica.ToString()), new Final(replicaStatistics.LatencyMean));
                metrics.Add(MetricsTags.InstantReplicaLatencyStdDeviation.For(replica.ToString()), new Final(replicaStatistics.LatencyStdDeviation));
            }

            metrics.Add(MetricsTags.PenalizedLatencyMean.For(), new Final(penalizedStatistic.Global.LatencyMean));
            metrics.Add(MetricsTags.PenalizedLatencyStdDeviation.For(), new Final(penalizedStatistic.Global.LatencyStdDeviation));
            foreach (var (replica, replicaStatistics) in penalizedStatistic.Replicas)
            {
                metrics.Add(MetricsTags.PenalizedReplicaLatencyMean.For(replica.ToString()), new Final(replicaStatistics.LatencyMean));
                metrics.Add(MetricsTags.PenalizedReplicaLatencyStdDeviation.For(replica.ToString()), new Final(replicaStatistics.LatencyStdDeviation));
            }

            metrics.Add(MetricsTags.SmoothedLatencyMean.For(), new Final(smoothedStatistic.Global.LatencyMean));
            metrics.Add(MetricsTags.SmoothedLatencyStdDeviation.For(), new Final(smoothedStatistic.Global.LatencyStdDeviation));
            foreach (var (replica, replicaStatistics) in smoothedStatistic.Replicas)
            {
                metrics.Add(MetricsTags.SmoothedReplicaLatencyMean.For(replica.ToString()), new Final(replicaStatistics.LatencyMean));
                metrics.Add(MetricsTags.SmoothedReplicaLatencyStdDeviation.For(replica.ToString()), new Final(replicaStatistics.LatencyStdDeviation));
            }

            foreach (var (replica, weight) in instantWeights)
                metrics.Add(MetricsTags.InstantReplicaWeight.For(replica.ToString()), new Final(weight.Value));
            foreach (var (replica, weight) in instantWeights2)
                metrics.Add(MetricsTags.InstantReplicaWeight2.For(replica.ToString()), new Final(weight.Value));
            foreach (var (replica, weight) in smoothedWeights)
                metrics.Add(MetricsTags.SmoothedReplicaWeight.For(replica.ToString()), new Final(weight.Value));

            settings.History.Put(topology, smoothedStatistic);
            settings.History.Put(topology, smoothedWeights);

            statisticsCollector.Put(topology, instantStatistics, smoothedStatistic, accumulatedStatistic, smoothedWeights);
        }

        private AvailabilityStatistics BuildAvailabilityStatistics(DateTimeOffset timestamp) =>
            new AvailabilityStatistics(
                topology,
                timestamp,
                GetLongCount(MetricsTags.AvailabilitySuccessResponses.For()),
                GetLongCount(MetricsTags.AvailabilityWarningResponses.For()),
                GetLongCount(MetricsTags.AvailabilityErrorResponses.For()));

        private Statistics BuildInstantStatistics(DateTimeOffset timestamp)
        {
            var instantGlobalStatistic = new ReplicaStatistics(
                timestamp,
                GetCount(MetricsTags.InstantSuccessResponses.For()),
                GetCount(MetricsTags.InstantWarningResponses.For()),
                GetCount(MetricsTags.InstantErrorResponses.For()),
                GetCount(MetricsTags.InstantSuccessLatency.For()),
                GetCount(MetricsTags.InstantWarningLatency.For()),
                GetCount(MetricsTags.InstantErrorLatency.For()),
                GetCount(MetricsTags.InstantSuccessSquaredLatency.For()),
                GetCount(MetricsTags.InstantWarningSquaredLatency.For()),
                GetCount(MetricsTags.InstantErrorSquaredLatency.For()));

            var instantStatistics = new Statistics
            {
                Global = instantGlobalStatistic
            };

            var replicas = SelectReplicas();
            foreach (var key in replicas)
            {
                var replica = key.ToString();
                var instantReplicaStatistic = new ReplicaStatistics(
                    timestamp,
                    GetCount(MetricsTags.InstantReplicaSuccessResponses.For(replica)),
                    GetCount(MetricsTags.InstantReplicaWarningResponses.For(replica)),
                    GetCount(MetricsTags.InstantReplicaErrorResponses.For(replica)),
                    GetCount(MetricsTags.InstantReplicaSuccessLatency.For(replica)),
                    GetCount(MetricsTags.InstantReplicaWarningLatency.For(replica)),
                    GetCount(MetricsTags.InstantReplicaErrorLatency.For(replica)),
                    GetCount(MetricsTags.InstantReplicaSuccessSquaredLatency.For(replica)),
                    GetCount(MetricsTags.InstantReplicaWarningSquaredLatency.For(replica)),
                    GetCount(MetricsTags.InstantReplicaErrorSquaredLatency.For(replica)));

                instantStatistics.Replicas[key] = instantReplicaStatistic;
            }

            return instantStatistics;
        }

        private Weights BuildInstantWeightsByLatencies(DateTimeOffset timestamp, Statistics statistics)
        {
            var weights = new Weights();

            foreach (var pair in statistics.Replicas)
            {
                var sensitivity = serviceSettings.Sensitivity;
                var weight = WeightCalculator.ComputeWeightByLatencies(statistics.Global, pair.Value, sensitivity);
                weights[pair.Key] = new ReplicaWeight(timestamp, weight);
            }

            weights.Sanitize();
            weights.NormalizeAndEnforceLimits(serviceSettings.MinimumWeight);

            return weights;
        }

        private Weights BuildInstantWeightsByLatencies2(DateTimeOffset timestamp, Statistics statistics)
        {
            var weights = new Weights();

            foreach (var pair in statistics.Replicas)
            {
                var sensitivity = serviceSettings.Sensitivity;
                var weight = WeightCalculator.ComputeWeightByLatencies2(statistics.Global, pair.Value, sensitivity);
                weights[pair.Key] = new ReplicaWeight(timestamp, weight);
            }

            weights.Sanitize();
            weights.NormalizeAndEnforceLimits(serviceSettings.MinimumWeight);

            return weights;
        }

        private Weights BuildInstantWeightsByStatuses(DateTimeOffset timestamp, Statistics statistics)
        {
            var weights = new Weights();

            foreach (var pair in statistics.Replicas)
            {
                var sensitivity = serviceSettings.Sensitivity;
                var weight = WeightCalculator.ComputeWeightByStatuses(statistics.Global, pair.Value, sensitivity);
                weights[pair.Key] = new ReplicaWeight(timestamp, weight);
            }

            weights.Sanitize();
            weights.NormalizeAndEnforceLimits(serviceSettings.MinimumWeight);

            return weights;
        }

        private void AddCounter(MetricTags key, double value, bool scaleToSeconds = true) =>
            GetOrCreateMetric(key, true, scaleToSeconds).Add(value);

        private void AddTimer(MetricTags key, double value) =>
            GetOrCreateMetric(key, false).Add(value);

        private IMetric GetOrCreateMetric(MetricTags key, bool isCounter, bool scaleToSeconds = true)
        {
            if (metrics.TryGetValue(key, out var result))
                return result;

            result = isCounter
                ? new Counter(scaleToSeconds)
                : settings.UseHistogram
                    ? (IMetric)new Histogram()
                    : new Timer();

            metrics[key] = result;

            return result;
        }

        private bool ShouldBeFiltered(MetricTags metricTags, HashSet<string> topClients, HashSet<string> topUrls)
        {
            var type = GetType(metricTags);
            if (type == GetType(MetricsTags.RpsPerUnescapedReplica.For("x")))
                return true;

            var client = GetClient(metricTags);
            if (client != null && !topClients.Contains(client))
                return true;

            var url = GetUrl(metricTags);
            if (url != null && !topUrls.Contains(url))
                return true;

            return false;
        }

        private string GetType(MetricTags tags) =>
            tags.FirstOrDefault(t => t.Key == "type")?.Value;

        private string GetClient(MetricTags tags) =>
            tags.FirstOrDefault(t => t.Key == "client")?.Value;

        private string GetUrl(MetricTags tags) =>
            tags.FirstOrDefault(t => t.Key == "url")?.Value;

        private HashSet<string> SelectTopClients() =>
            SelectTop(
                GetType(MetricsTags.RpsPerClient.For("x")),
                "client",
                serviceSettings.ClientsLimit);

        private HashSet<string> SelectTopUrls() =>
            SelectTop(
                GetType(MetricsTags.RpsPerUrl.For("x")),
                "url",
                serviceSettings.UrlsLimit);

        private HashSet<TopologyReplica> SelectReplicas()
        {
            var encoded = SelectTop(
                GetType(MetricsTags.RpsPerUnescapedReplica.For("x")),
                "replica",
                int.MaxValue);

            var result = new HashSet<TopologyReplica>();

            foreach (var e in encoded)
            {
                var tokens = e.Split(':');
                if (tokens.Length != 2 || !int.TryParse(tokens[1], out var port))
                    continue;

                result.Add(new TopologyReplica(tokens[0], port, string.Empty));
            }

            return result;
        }

        private HashSet<string> SelectTop(string type, string key, int count)
        {
            var keys = metrics.Keys.Where(
                k => k.Any(tag => tag.Key == "type" && tag.Value == type) && k.Count == 2);

            var topKeys = keys.OrderByDescending(k => ((Counter)metrics[k]).GetValue()).Take(count);

            return new HashSet<string>(topKeys.Select(k => k.First(kk => kk.Key == key).Value));
        }

        private void LogRps(ILog log, DateTimeOffset timestamp)
        {
            var rps = MetricsHelper.Scale(GetCount(MetricsTags.Rps.For()) ?? 0);
            if (rps > 1_000)
                log.Debug(
                    "Writing metric for service '{Service}' at environment '{Environment}': rps={Rps}, timestamp={Timestamp}.",
                    topology.Service,
                    topology.Environment,
                    rps,
                    timestamp.ToString("T"));

            if (rps > 10_000 && !settings.UseHistogram)
                log.Error(
                    "Too much spans for service '{Service}' at environment '{Environment}': rps={Rps}, timestamp={Timestamp}.",
                    topology.Service,
                    topology.Environment,
                    rps,
                    timestamp.ToString("T"));
        }

        private void LogSent(ILog log, DateTimeOffset timestamp, int sent)
        {
            if (sent > 1_000)
                log.Debug(
                    "Written {Count} metrics for service '{Service}' at environment '{Environment}', timestamp={Timestamp}.",
                    sent,
                    topology.Service,
                    topology.Environment,
                    timestamp.ToString("T"));
        }

        private long GetLongCount(MetricTags tags) =>
            (long)(GetCount(tags) ?? 0);

        private double? GetCount(MetricTags tags)
        {
            if (!metrics.TryGetValue(tags, out var result))
                return null;

            return ((Counter)result).GetValue();
        }
    }
}