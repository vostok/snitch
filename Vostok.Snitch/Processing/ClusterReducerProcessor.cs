using System;
using Vostok.Hercules.Consumers;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Events;
using Vostok.Snitch.Metrics;

namespace Vostok.Snitch.Processing
{
    internal class ClusterReducerProcessor : WindowedStreamConsumerSettings<AggregatedEvent, TopologyKey>.IWindow
    {
        private readonly TopologyKey topologyKey;
        private readonly ClusterReducerProcessorSettings settings;
        private readonly MetricsProcessor metricsProcessor;

        public ClusterReducerProcessor(TopologyKey topologyKey, ClusterReducerProcessorSettings settings)
        {
            this.topologyKey = topologyKey;
            this.settings = settings;

            metricsProcessor = new MetricsProcessor(topologyKey, settings.MetricsSettings);
        }

        public void Add(AggregatedEvent @event) =>
            metricsProcessor.Add(@event);

        public void Flush(DateTimeOffset timestamp) =>
            metricsProcessor.Write(settings.MetricContext, settings.StatisticsCollector, settings.Log, timestamp);
    }
}