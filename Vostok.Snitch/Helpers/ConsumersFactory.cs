using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Ordering.Weighed;
using Vostok.Clusterclient.Core.Strategies;
using Vostok.Clusterclient.Core.Topology;
using Vostok.ClusterClient.Datacenters;
using Vostok.Clusterclient.Snitch;
using Vostok.Clusterclient.Topology.SD;
using Vostok.Clusterclient.Tracing;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Datacenters;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Metrics.Primitives.Gauge;
using Vostok.Snitch.AggregatedEvents;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Storages;
using Vostok.Tracing.Hercules.Models;
using Vostok.Tracing.Hercules.Readers;

namespace Vostok.Snitch.Helpers
{
    public static class ConsumersFactory
    {
        public static BatchesStreamConsumer<HerculesHttpClientSpan> CreateBatchesStreamConsumer(
            IVostokHostingEnvironment environment,
            string streamName,
            Action<HerculesHttpClientSpan> onEvent) =>
            new BatchesStreamConsumer<HerculesHttpClientSpan>(
                new BatchesStreamConsumerSettings<HerculesHttpClientSpan>(
                    streamName,
                    () => environment.SecretConfigurationProvider.Get<SnitchConsumerSecretSettings>().HerculesApiKey,
                    CreateStreamApiCluster(environment),
                    r => new HerculesHttpClientSpanReader(),
                    environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.CoordinatesStorageKey),
                    () => new StreamShardingSettings(environment.ApplicationReplicationInfo.InstanceIndex, environment.ApplicationReplicationInfo.InstancesCount)
                )
                {
                    OnEvent = (e, _) => onEvent(e),
                    EventsReadBatchSize = environment.ConfigurationProvider.Get<ConsumerSettings>().EventsReadBatchSize,
                    MetricContext = environment.Metrics.Instance,
                    StreamApiClientAdditionalSetup = ClusterClientAdditionalSetup(environment)
                },
                environment.Log);

        public static WindowedStreamConsumer<HerculesHttpClientSpan, TopologyKey> CreateWindowedStreamConsumer(
            IVostokHostingEnvironment environment,
            string streamName,
            Func<TopologyKey, WindowedStreamConsumerSettings<HerculesHttpClientSpan, TopologyKey>.IWindow> createWindow,
            StreamBinaryEventsWriter eventsWriter,
            ITopologyStatisticsWriter statisticsWriter)
        {
            var settings = new WindowedStreamConsumerSettings<HerculesHttpClientSpan, TopologyKey>(
                streamName,
                () => environment.SecretConfigurationProvider.Get<SnitchConsumerSecretSettings>().HerculesApiKey,
                CreateStreamApiCluster(environment),
                s => new TopologyKey(s.TargetEnvironment, s.TargetService),
                s => s.EndTimestamp,
                createWindow,
                r => new HerculesHttpClientSpanReader(),
                environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.LeftCoordinatesStorageKey),
                environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.RightCoordinatesStorageKey),
                () => new StreamShardingSettings(environment.ApplicationReplicationInfo.InstanceIndex, environment.ApplicationReplicationInfo.InstancesCount)
            );

            SetupCommonSettings(environment, settings, eventsWriter, statisticsWriter);

            return new WindowedStreamConsumer<HerculesHttpClientSpan, TopologyKey>(settings, environment.Log);
        }

        public static WindowedStreamConsumer<HerculesHttpClusterSpan, TopologyKey> CreateWindowedStreamConsumer(
            IVostokHostingEnvironment environment,
            string streamName,
            Func<TopologyKey, WindowedStreamConsumerSettings<HerculesHttpClusterSpan, TopologyKey>.IWindow> createWindow,
            StreamBinaryEventsWriter eventsWriter,
            ITopologyStatisticsWriter statisticsWriter)
        {
            var settings = new WindowedStreamConsumerSettings<HerculesHttpClusterSpan, TopologyKey>(
                streamName,
                () => environment.SecretConfigurationProvider.Get<SnitchConsumerSecretSettings>().HerculesApiKey,
                CreateStreamApiCluster(environment),
                s => new TopologyKey(s.TargetEnvironment, s.TargetService),
                s => s.EndTimestamp,
                createWindow,
                r => new HerculesHttpClusterSpanReader(),
                environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.LeftCoordinatesStorageKey),
                environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.RightCoordinatesStorageKey),
                () => new StreamShardingSettings(environment.ApplicationReplicationInfo.InstanceIndex, environment.ApplicationReplicationInfo.InstancesCount)
            );

            SetupCommonSettings(environment, settings, eventsWriter, statisticsWriter);

            return new WindowedStreamConsumer<HerculesHttpClusterSpan, TopologyKey>(settings, environment.Log);
        }

        public static WindowedStreamConsumer<AggregatedEvent, TopologyKey> CreateWindowedStreamConsumer(
            IVostokHostingEnvironment environment,
            string streamName,
            Func<TopologyKey, WindowedStreamConsumerSettings<AggregatedEvent, TopologyKey>.IWindow> createWindow,
            StreamBinaryEventsWriter eventsWriter,
            ITopologyStatisticsWriter statisticsWriter)
        {
            var settings = new WindowedStreamConsumerSettings<AggregatedEvent, TopologyKey>(
                streamName,
                () => environment.SecretConfigurationProvider.Get<SnitchConsumerSecretSettings>().HerculesApiKey,
                CreateStreamApiCluster(environment),
                s => new TopologyKey(s.TargetEnvironment, s.TargetService),
                s => s.Timestamp,
                createWindow,
                r => new AggregatedEventReader(r),
                environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.LeftCoordinatesStorageKey),
                environment.HostExtensions.Get<IStreamCoordinatesStorage>(Constants.RightCoordinatesStorageKey),
                () => new StreamShardingSettings(environment.ApplicationReplicationInfo.InstanceIndex, environment.ApplicationReplicationInfo.InstancesCount)
            );

            SetupCommonSettings(environment, settings, eventsWriter, statisticsWriter);

            return new WindowedStreamConsumer<AggregatedEvent, TopologyKey>(settings, environment.Log);
        }

        public static StreamBinaryWriter CreateStreamBinaryWriter(IVostokHostingEnvironment environment) =>
            new StreamBinaryWriter(
                new StreamBinaryWriterSettings(
                    () => environment.SecretConfigurationProvider.Get<SnitchConsumerSecretSettings>().HerculesApiKey,
                    CreateGateCluster(environment))
                {
                    MetricContext = environment.Metrics.Instance,
                    GateClientAdditionalSetup = ClusterClientAdditionalSetup(environment)
                },
                environment.Log);

        public static StreamBinaryEventsWriter CreateStreamBinaryEventsWriter(
            IVostokHostingEnvironment environment,
            string streamName) =>
            new StreamBinaryEventsWriter(
                new StreamBinaryEventsWriterSettings(
                    CreateStreamBinaryWriter(environment),
                    streamName)
                {
                    BufferCapacityLimit = environment.ConfigurationProvider.Get<ConsumerSettings>().EventsWriteBufferCapacityLimit
                },
                environment.Log);

        public static ClusterClientSetup ClusterClientAdditionalSetup(IVostokHostingEnvironment environment) =>
            configuration =>
            {
                var service = configuration.TargetServiceName == "Hercules.Gate"
                    ? environment.ConfigurationProvider.Get<HerculesSettings>().HerculesGateTopology
                    : environment.ConfigurationProvider.Get<HerculesSettings>().HerculesStreamApiTopology;

                configuration.SetupDistributedTracing(environment.Tracer);
                configuration.SetupWeighedReplicaOrdering(
                    weightOrdering =>
                    {
                        weightOrdering.AddAdaptiveHealthModifierWithLinearDecay(TimeSpan.FromMinutes(10));
                        weightOrdering.SetupAvoidInactiveDatacentersWeightModifier(DatacentersProvider.Get());
                        weightOrdering.SetupBoostLocalDatacentersWeightModifier(
                            DatacentersProvider.Get(),
                            () => environment.ConfigurationProvider.Get<HerculesSettings>().BoostLocalDatacenterMultiplier,
                            () => environment.ConfigurationProvider.Get<HerculesSettings>().MinimumWeightForBoostingLocalDatacenter);
                        weightOrdering.SetupSnitchWeightModifier(
                            environment.ServiceLocator, 
                            service,
                            environment.ConfigurationProvider.Get<HerculesSettings>().HerculesEnvironmentTopology);
                    });
                configuration.DefaultRequestStrategy = Strategy.Sequential1;
            };

        

        private static void SetupCommonSettings<T, TKey>(
            IVostokHostingEnvironment environment,
            WindowedStreamConsumerSettings<T, TKey> settings,
            StreamBinaryEventsWriter eventsWriter,
            ITopologyStatisticsWriter statisticsWriter)
        {
            Task writeTask = null;

            void OnBatchBegin(StreamCoordinates _) =>
                writeTask?.GetAwaiter().GetResult();

            void OnBatchEnd(StreamCoordinates _) =>
                writeTask = WriteAsync(eventsWriter, statisticsWriter);

            settings.EventsReadBatchSize = environment.ConfigurationProvider.Get<ConsumerSettings>().EventsReadBatchSize;
            settings.MetricContext = environment.Metrics.Instance;
            settings.StreamApiClientAdditionalSetup = ClusterClientAdditionalSetup(environment);
            settings.Period = Constants.Period;
            settings.Lag = environment.ConfigurationProvider.Get<ConsumerSettings>().Lag;
            settings.MaximumDeltaAfterNow = environment.ConfigurationProvider.Get<ConsumerSettings>().MaximumDeltaAfterNow;
            settings.OnBatchBegin = OnBatchBegin;
            settings.OnBatchEnd = OnBatchEnd;
        }

        private static Task WriteAsync(StreamBinaryEventsWriter eventsWriter, ITopologyStatisticsWriter statisticsWriter)
        {
            var tasks = new List<Task>();

            if (eventsWriter != null)
                tasks.Add(eventsWriter.WriteAsync());

            if (statisticsWriter != null)
                tasks.Add(statisticsWriter.WriteAsync());

            return Task.WhenAll(tasks).SilentlyContinue();
        }

        private static IClusterProvider CreateStreamApiCluster(IVostokHostingEnvironment environment) =>
            new ServiceDiscoveryClusterProvider(
                environment.ServiceLocator,
                environment.ConfigurationProvider.Get<HerculesSettings>().HerculesEnvironmentTopology,
                environment.ConfigurationProvider.Get<HerculesSettings>().HerculesStreamApiTopology,
                environment.Log);

        private static IClusterProvider CreateGateCluster(IVostokHostingEnvironment environment) =>
            new ServiceDiscoveryClusterProvider(
                environment.ServiceLocator,
                environment.ConfigurationProvider.Get<HerculesSettings>().HerculesEnvironmentTopology,
                environment.ConfigurationProvider.Get<HerculesSettings>().HerculesGateTopology,
                environment.Log);
    }
}