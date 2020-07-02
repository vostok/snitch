using Vostok.Metrics.Models;
using Vostok.Snitch.AggregatedEvents;

namespace Vostok.Snitch.Metrics.Models
{
    internal interface IMetric
    {
        void Add(AggregatedEvent @event);

        void Add(double value);

        double[] ToAggregatedEventValues();

        MetricDataPoint[] ToMetricDataPoints();
    }
}