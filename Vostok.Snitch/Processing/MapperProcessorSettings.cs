using Vostok.Hercules.Consumers;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Metrics;

namespace Vostok.Snitch.Processing
{
    internal class MapperProcessorSettings
    {
        public readonly StreamBinaryEventsWriter EventsWriter;
        public readonly ILog Log;
        public readonly MetricsProcessorSettings MetricsSettings;

        public MapperProcessorSettings(StreamBinaryEventsWriter eventsWriter, ILog log, MetricsProcessorSettings metricsSettings)
        {
            EventsWriter = eventsWriter;
            Log = log;
            MetricsSettings = metricsSettings;
        }
    }
}