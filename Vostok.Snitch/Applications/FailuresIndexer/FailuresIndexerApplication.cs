using System;
using System.Threading.Tasks;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Metrics.Grouping;
using Vostok.Metrics.Primitives.Gauge;
using Vostok.Metrics.Primitives.Timer;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Helpers;
using Vostok.Snitch.Storages;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications.FailuresIndexer
{
    [RequiresSecretConfiguration(typeof(SnitchConsumerSecretSettings))]
    [RequiresConfiguration(typeof(FailuresIndexerSettings))]
    public class FailuresIndexerApplication : IVostokApplication
    {
        private BatchesStreamConsumer<HerculesHttpClientSpan> consumer;
        private TargetsFailures failures;
        private IFailuresStorage errorsStorage, warningsStorage, slowsStorage;
        private DateTime flushed;
        private FailuresIndexerSettings settings;

        private IMetricGroup1<IIntegerGauge> eventsMetric;
        private IMetricGroup1<ITimer> iterationMetric;

        public Task InitializeAsync(IVostokHostingEnvironment environment)
        {
            settings = environment.ConfigurationProvider.Get<FailuresIndexerSettings>();

            consumer = ConsumersFactory.CreateBatchesStreamConsumer(environment, settings.SourceStream, OnEvent);

            failures = new TargetsFailures(new TargetsFailuresSettings());

            (errorsStorage, warningsStorage, slowsStorage) = environment.HostExtensions.Get<(IFailuresStorage errors, IFailuresStorage warns, IFailuresStorage slows)>();

            flushed = DateTime.UtcNow;

            eventsMetric = environment.Metrics.Instance.CreateIntegerGauge("events", "type", new IntegerGaugeConfig {ResetOnScrape = true});
            iterationMetric = environment.Metrics.Instance.CreateSummary("iteration", "type", new SummaryConfig {Quantiles = new[] {0.5, 0.75, 1}});

            return Task.CompletedTask;
        }

        public Task RunAsync(IVostokHostingEnvironment environment) =>
            consumer.RunAsync(environment.ShutdownToken);

        private void OnEvent(HerculesHttpClientSpan span)
        {
            failures.Add(span);

            TryFlush();
        }

        private void TryFlush()
        {
            if (DateTime.UtcNow - flushed < settings.FlushPeriod)
                return;

            flushed = DateTime.UtcNow;

            using (iterationMetric?.For("write_time").Measure())
            {
                var errors = failures.CollectErrors();
                var warnings = failures.CollectWarnings();
                var slows = failures.CollectSlows();

                Task.WaitAll(
                    errorsStorage.WriteAsync(errors),
                    warningsStorage.WriteAsync(warnings),
                    slowsStorage.WriteAsync(slows));

                eventsMetric?.For("out").Add(errors.Count + warnings.Count + slows.Count);
            }
        }
    }
}