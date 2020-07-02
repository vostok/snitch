using System;
using Vostok.Hercules.Consumers;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications.ClusterMapper
{
    internal class ClusterMapperProcessor : WindowedStreamConsumerSettings<HerculesHttpClusterSpan, TopologyKey>.IWindow
    {
        private readonly TopologyKey topology;
        private readonly ClusterMapperProcessorSettings settings;
        private readonly MetricsProcessor metricsProcessor;

        public ClusterMapperProcessor(TopologyKey topology, ClusterMapperProcessorSettings settings)
        {
            this.topology = topology;
            this.settings = settings;

            metricsProcessor = new MetricsProcessor(topology, settings.MetricsSettings);
        }

        public void Add(HerculesHttpClusterSpan span) =>
            metricsProcessor.Add(span);

        public void Flush(DateTimeOffset timestamp) =>
            metricsProcessor.Write(settings.EventsWriter, settings.Log, timestamp);
    }
}