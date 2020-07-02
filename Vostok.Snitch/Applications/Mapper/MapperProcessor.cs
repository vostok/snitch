using System;
using Vostok.Hercules.Consumers;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications.Mapper
{
    internal class MapperProcessor : WindowedStreamConsumerSettings<HerculesHttpClientSpan, TopologyKey>.IWindow
    {
        private readonly TopologyKey topology;
        private readonly MapperProcessorSettings settings;
        private readonly MetricsProcessor metricsProcessor;

        public MapperProcessor(TopologyKey topology, MapperProcessorSettings settings)
        {
            this.topology = topology;
            this.settings = settings;

            metricsProcessor = new MetricsProcessor(topology, settings.MetricsSettings);
        }

        public void Add(HerculesHttpClientSpan @event) =>
            metricsProcessor.Add(@event);

        public void Flush(DateTimeOffset timestamp) =>
            metricsProcessor.Write(settings.EventsWriter, settings.Log, timestamp);
    }
}