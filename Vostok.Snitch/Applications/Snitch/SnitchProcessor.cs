using System;
using Vostok.Hercules.Consumers;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications.Snitch
{
    internal class SnitchProcessor : WindowedStreamConsumerSettings<HerculesHttpClientSpan, TopologyKey>.IWindow
    {
        private readonly TopologyKey topologyKey;
        private readonly SnitchProcessorSettings settings;
        private readonly MetricsProcessor metricsProcessor;

        public SnitchProcessor(TopologyKey topologyKey, SnitchProcessorSettings settings)
        {
            this.topologyKey = topologyKey;
            this.settings = settings;

            metricsProcessor = new MetricsProcessor(topologyKey, settings.MetricsSettings);
        }

        public void Add(HerculesHttpClientSpan @event) =>
            metricsProcessor.Add(@event);

        public void Flush(DateTimeOffset timestamp) =>
            metricsProcessor.Write(settings.MetricContext, settings.StatisticsCollector, settings.Log, timestamp);
    }
}