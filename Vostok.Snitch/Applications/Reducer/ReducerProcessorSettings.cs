using Vostok.Logging.Abstractions;
using Vostok.Metrics;
using Vostok.Snitch.Metrics;

namespace Vostok.Snitch.Applications.Reducer
{
    internal class ReducerProcessorSettings
    {
        public readonly IMetricContext MetricContext;
        public readonly TopologyStatisticsCollector StatisticsCollector;
        public readonly ILog Log;
        public readonly MetricsProcessorSettings MetricsSettings;

        public ReducerProcessorSettings(IMetricContext metricContext, TopologyStatisticsCollector statisticsCollector, ILog log, MetricsProcessorSettings metricsSettings)
        {
            MetricContext = metricContext;
            StatisticsCollector = statisticsCollector;
            Log = log;
            MetricsSettings = metricsSettings;
        }
    }
}