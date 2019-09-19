using System;
using JetBrains.Annotations;
using Vostok.Metrics;

namespace Vostok.Snitch.Processors
{
    [PublicAPI]
    public class SnitchMetricsProcessorSettings
    {
        public SnitchMetricsProcessorSettings([NotNull] IMetricContext metricContext)
        {
            MetricContext = metricContext ?? throw new ArgumentNullException(nameof(metricContext));
        }

        [NotNull]
        public IMetricContext MetricContext { get; }
    }
}