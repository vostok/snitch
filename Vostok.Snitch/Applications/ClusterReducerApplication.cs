using System.Threading.Tasks;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.AggregatedEvents;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Helpers;
using Vostok.Snitch.Metrics;
using Vostok.Snitch.Processing;
using Vostok.Snitch.Storages;

namespace Vostok.Snitch.Applications
{
    [RequiresConfiguration(typeof(ClusterReducerSettings))]
    public class ClusterReducerApplication : SnitchConsumerBase
    {
        private WindowedStreamConsumer<AggregatedEvent, TopologyKey> consumer;

        protected override Task InitializeConsumerAsync(IVostokHostingEnvironment environment)
        {
            var settings = environment.ConfigurationProvider.Get<ClusterReducerSettings>();
            var metricsSettings = new MetricsProcessorSettings(true);

            var (metricContext, eventsWriter) = MetricContextFactory.Create(environment);

            var statisticsCollector = environment.HostExtensions.Get<TopologyStatisticsCollector>();
            var statisticsWriter = environment.HostExtensions.Get<ITopologyStatisticsWriter>();

            var snitchProcessorSettings = new ClusterReducerProcessorSettings(
                metricContext,
                statisticsCollector,
                environment.Log.ForContext<ClusterReducerProcessor>(),
                metricsSettings);

            consumer = ConsumersFactory.CreateWindowedStreamConsumer(
                environment,
                settings.SourceStream,
                key => new ClusterReducerProcessor(key, snitchProcessorSettings),
                eventsWriter,
                statisticsWriter);

            return Task.CompletedTask;
        }

        protected override Task RunConsumerAsync(IVostokHostingEnvironment environment) =>
            consumer.RunAsync(environment.ShutdownToken);
    }
}