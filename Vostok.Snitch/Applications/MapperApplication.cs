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
    [RequiresConfiguration(typeof(MapperSettings))]
    public class MapperApplication : SnitchApplicationBase
    {
        private WindowedStreamConsumer<HerculesHttpClientSpan, TopologyKey> consumer;

        public override Task InitializeAsync(IVostokHostingEnvironment environment)
        {
            ConsumersFactory.SetupEventsLimitMetric(environment, () => environment.ConfigurationProvider.Get<ConsumerSettings>().EventsLimitMetric);

            var settings = environment.ConfigurationProvider.Get<MapperSettings>();
            var metricsSettings = new MetricsProcessorSettings(true);

            var eventsWriter = ConsumersFactory.CreateStreamBinaryEventsWriter(environment, settings.TargetStream);

            var snitchProcessorSettings = new MapperProcessorSettings(
                eventsWriter,
                environment.Log.ForContext<MapperProcessor>(),
                metricsSettings);

            consumer = ConsumersFactory.CreateWindowedStreamConsumer(
                environment,
                settings.SourceStream,
                key => new MapperProcessor(key, snitchProcessorSettings),
                eventsWriter,
                null);

            return Task.CompletedTask;
        }

        public override Task RunAsync(IVostokHostingEnvironment environment) =>
            consumer.RunAsync(environment.ShutdownToken);
    }
}