using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Metrics;
using Vostok.Metrics.Models;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch.Processors
{
    [PublicAPI]
    public class SnitchMetricsProcessor : ISnitchProcessor
    {
        [NotNull]
        private readonly SnitchMetricsProcessorSettings settings;
        private readonly Target target;
        private readonly ILog log;
        private IMetricContext metricContext;

        public SnitchMetricsProcessor([NotNull] SnitchMetricsProcessorSettings settings, Target target, ILog log)
        {
            this.settings = settings;
            this.target = target;
            this.log = log;
            this.metricContext = settings.MetricContext.WithTag("target-environment", target.Environment).WithTag("target-service", target.Service);
        }

        public void Process(DateTime timestamp, IReadOnlyList<HerculesHttpSpan> spans)
        {
            metricContext.Send(new MetricDataPoint(spans.Count / 10.0, "rpsTotal") {Timestamp = timestamp});
        }
    }
}