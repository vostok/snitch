using System.Threading.Tasks;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Helpers;
using Vostok.Snitch.Metrics;
using Vostok.Snitch.Processing;
using Vostok.Snitch.Storage;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications
{
    [RequiresConfiguration(typeof(SnitchSettings))]
    public class SnitchApplication : SnitchApplicationBase
    {
        private WindowedStreamConsumer<HerculesHttpClientSpan, TopologyKey> consumer;
        
        public override Task InitializeAsync(IVostokHostingEnvironment environment)
        {
            ConsumersFactory.SetupEventsLimitMetric(environment, () => environment.ConfigurationProvider.Get<ConsumerSettings>().EventsLimitMetric);

            var settings = environment.ConfigurationProvider.Get<SnitchSettings>();
            var metricsSettings = new MetricsProcessorSettings(false);

            var (metricContext, eventsWriter) = MetricContextFactory.Create(environment);

            var statisticsCollector = environment.HostExtensions.Get<TopologyStatisticsCollector>();
            var statisticsWriter = environment.HostExtensions.Get<ITopologyStatisticsWriter>();

            var snitchProcessorSettings = new SnitchProcessorSettings(
                metricContext,
                statisticsCollector,
                environment.Log.ForContext<SnitchProcessor>(),
                metricsSettings);

            consumer = ConsumersFactory.CreateWindowedStreamConsumer(
                environment,
                settings.SourceStream,
                key => new SnitchProcessor(key, snitchProcessorSettings),
                eventsWriter,
                statisticsWriter);

            return Task.CompletedTask;
        }

        public override Task RunAsync(IVostokHostingEnvironment environment) =>
            consumer.RunAsync(environment.ShutdownToken);
    }
}