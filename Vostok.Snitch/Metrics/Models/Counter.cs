using Vostok.Metrics.Models;
using Vostok.Snitch.AggregatedEvents;

// ReSharper disable ParameterHidesMember

namespace Vostok.Snitch.Metrics.Models
{
    internal class Counter : IMetric
    {
        private readonly bool scaleToSeconds;
        private double value;

        public Counter(in bool scaleToSeconds) =>
            this.scaleToSeconds = scaleToSeconds;

        public void Add(AggregatedEvent @event)
        {
            if (@event.Values.Length > 0)
                value += @event.Values[0];
        }

        public void Add(double value) =>
            this.value += value;

        public double[] ToAggregatedEventValues() =>
            new[] {value};

        public MetricDataPoint[] ToMetricDataPoints() =>
            new[] {new MetricDataPoint(scaleToSeconds ? MetricsHelper.Scale(value) : value)};

        public double GetValue() => value;

        public override string ToString() =>
            $"{nameof(value)}: {value}";
    }
}