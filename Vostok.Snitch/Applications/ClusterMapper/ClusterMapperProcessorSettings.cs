using Vostok.Hercules.Consumers;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Metrics;

namespace Vostok.Snitch.Applications.ClusterMapper
{
    internal class ClusterMapperProcessorSettings
    {
        public readonly StreamBinaryEventsWriter EventsWriter;
        public readonly ILog Log;
        public readonly MetricsProcessorSettings MetricsSettings;

        public ClusterMapperProcessorSettings(StreamBinaryEventsWriter eventsWriter, ILog log, MetricsProcessorSettings metricsSettings)
        {
            EventsWriter = eventsWriter;
            Log = log;
            MetricsSettings = metricsSettings;
        }
    }
}