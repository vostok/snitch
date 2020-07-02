using System;
using Vostok.Hercules.Consumers;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Processing
{
    internal class ClusterSnitchProcessor : WindowedStreamConsumerSettings<HerculesHttpClusterSpan, TopologyKey>.IWindow
    {
        private readonly TopologyKey topologyKey;
        private readonly ClusterSnitchProcessorSettings settings;
        private readonly MetricsProcessor metricsProcessor;

        public ClusterSnitchProcessor(TopologyKey topologyKey, ClusterSnitchProcessorSettings settings)
        {
            this.topologyKey = topologyKey;
            this.settings = settings;

            metricsProcessor = new MetricsProcessor(topologyKey, settings.MetricsSettings);
        }

        public void Add(HerculesHttpClusterSpan @event)
        {
            metricsProcessor.Add(@event);
        }

        public void Flush(DateTimeOffset timestamp)
        {
            metricsProcessor.Write(settings.MetricContext, settings.StatisticsCollector, settings.Log, timestamp);
        }
    }
}