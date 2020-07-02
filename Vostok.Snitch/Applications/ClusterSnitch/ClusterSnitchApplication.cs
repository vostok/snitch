using System.Threading.Tasks;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Helpers;
using Vostok.Snitch.Metrics;
using Vostok.Snitch.Storages;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications.ClusterSnitch
{
    [RequiresConfiguration(typeof(ClusterSnitchSettings))]
    public class ClusterSnitchApplication : SnitchConsumerBase
    {
        private WindowedStreamConsumer<HerculesHttpClusterSpan, TopologyKey> consumer;

        protected override Task InitializeConsumerAsync(IVostokHostingEnvironment environment)
        {
            var settings = environment.ConfigurationProvider.Get<ClusterSnitchSettings>();
            var metricsSettings = new MetricsProcessorSettings(false);

            var (metricContext, eventsWriter) = MetricContextFactory.Create(environment);

            var statisticsCollector = environment.HostExtensions.Get<TopologyStatisticsCollector>();
            var statisticsWriter = environment.HostExtensions.Get<ITopologyStatisticsWriter>();

            var snitchProcessorSettings = new ClusterSnitchProcessorSettings(
                metricContext,
                statisticsCollector,
                environment.Log.ForContext<ClusterSnitchProcessor>(),
                metricsSettings);

            consumer = ConsumersFactory.CreateWindowedStreamConsumer(
                environment,
                settings.SourceStream,
                key => new ClusterSnitchProcessor(key, snitchProcessorSettings),
                eventsWriter,
                statisticsWriter);

            return Task.CompletedTask;
        }

        protected override Task RunConsumerAsync(IVostokHostingEnvironment environment) =>
            consumer.RunAsync(environment.ShutdownToken);
    }
}