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
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications
{
    [RequiresConfiguration(typeof(ClusterMapperSettings))]
    public class ClusterMapperApplication : SnitchConsumerBase
    {
        private WindowedStreamConsumer<HerculesHttpClusterSpan, TopologyKey> consumer;

        protected override Task InitializeConsumerAsync(IVostokHostingEnvironment environment)
        {
            var settings = environment.ConfigurationProvider.Get<ClusterMapperSettings>();
            var metricsSettings = new MetricsProcessorSettings(true);

            var eventsWriter = ConsumersFactory.CreateStreamBinaryEventsWriter(environment, settings.TargetStream);

            var snitchProcessorSettings = new ClusterMapperProcessorSettings(
                eventsWriter,
                environment.Log.ForContext<ClusterMapperProcessor>(),
                metricsSettings);

            consumer = ConsumersFactory.CreateWindowedStreamConsumer(
                environment,
                settings.SourceStream,
                key => new ClusterMapperProcessor(key, snitchProcessorSettings),
                eventsWriter,
                null);

            return Task.CompletedTask;
        }

        protected override Task RunConsumerAsync(IVostokHostingEnvironment environment) =>
            consumer.RunAsync(environment.ShutdownToken);
    }
}