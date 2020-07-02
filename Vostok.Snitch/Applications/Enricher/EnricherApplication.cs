using System.Threading.Tasks;
using Vostok.Clusterclient.Topology.SD;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Metrics.Grouping;
using Vostok.Metrics.Primitives.Gauge;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Core;
using Vostok.Snitch.Core.Classifiers;
using Vostok.Snitch.Core.Topologies;
using Vostok.Snitch.Helpers;

namespace Vostok.Snitch.Applications.Enricher
{
    [RequiresSecretConfiguration(typeof(SnitchConsumerSecretSettings))]
    [RequiresConfiguration(typeof(EnricherSettings))]
    public class EnricherApplication : IVostokApplication
    {
        private BatchesStreamConsumer<SpanProcessResult> consumer;

        private IMetricGroup1<IIntegerGauge> eventsMetric;
        private CompositeSpanBinaryWriter spanWriter;

        private volatile Task writeTask;

        public Task InitializeAsync(IVostokHostingEnvironment environment)
        {
            var settings = environment.ConfigurationProvider.Get<EnricherSettings>();

            eventsMetric = environment.Metrics.Instance.CreateIntegerGauge("events", "type", new IntegerGaugeConfig {ResetOnScrape = true});

            var streamBinaryWriter = ConsumersFactory.CreateStreamBinaryWriter(environment);
            var responseCodeClassifier = new ResponseCodeClassifier(() => environment.ConfigurationProvider.Get<ResponseCodeClassifierSettings>());
            var latencyClassifier = new LatencyClassifier(() => environment.ConfigurationProvider.Get<LatencyClassifierSettings>());
            var urlNormalizer = new UrlNormalizer(() => environment.ConfigurationProvider.Get<UrlNormalizerSettings>());
            var topologiesResolver = new TopologiesResolver(
                new TopologiesResolverSettings(
                    environment.ClusterConfigClient,
                    environment.HostExtensions.Get<IServiceDiscoveryManager>())
                {
                    EnvironmentsWhitelist = () => environment.ConfigurationProvider.Get<EnvironmentsWhitelistSetting>()
                },
                environment.Log);
            topologiesResolver.Warmup();

            spanWriter = new CompositeSpanBinaryWriter(
                streamBinaryWriter,
                responseCodeClassifier,
                latencyClassifier,
                urlNormalizer,
                topologiesResolver);

            var batchesStreamConsumerSettings = new BatchesStreamConsumerSettings<SpanProcessResult>(
                settings.SourceStream,
                () => environment.SecretConfigurationProvider.Get<SnitchConsumerSecretSettings>().HerculesApiKey,
                new ServiceDiscoveryClusterProvider(environment.ServiceLocator, Constants.HerculesEnvironment, Constants.HerculesStreamApi, environment.Log),
                reader => new SpanReaderWriter(topologiesResolver, reader, spanWriter),
                environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.CoordinatesStorageKey),
                () => new StreamShardingSettings(environment.ApplicationReplicationInfo.InstanceIndex, environment.ApplicationReplicationInfo.InstancesCount))
            {
                EventsReadBatchSize = environment.ConfigurationProvider.Get<ConsumerSettings>().EventsReadBatchSize,
                MetricContext = environment.Metrics.Instance,
                StreamApiClientAdditionalSetup = ConsumersFactory.ClusterClientAdditionalSetup(environment),
                OnEvent = OnEvent,
                OnBatchBegin = OnBatchBegin,
                OnBatchEnd = OnBatchEnd
            };

            consumer = new BatchesStreamConsumer<SpanProcessResult>(batchesStreamConsumerSettings, environment.Log);

            return Task.CompletedTask;
        }

        public Task RunAsync(IVostokHostingEnvironment environment) => 
            consumer.RunAsync(environment.ShutdownToken);

        private void OnEvent(SpanProcessResult result, StreamCoordinates _)
        {
            if (result.WritesCount == 0)
                eventsMetric.For("skip").Increment();
            if (result.WritesCount > 1)
                eventsMetric.For("duplicated").Add(result.WritesCount - 1);
        }

        private void OnBatchBegin(StreamCoordinates _) => 
            writeTask?.GetAwaiter().GetResult();

        private void OnBatchEnd(StreamCoordinates _) => 
            writeTask = spanWriter.WriteAsync();
    }
}