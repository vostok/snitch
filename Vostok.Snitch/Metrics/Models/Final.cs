using System;
using Vostok.Metrics.Models;
using Vostok.Snitch.Events;

namespace Vostok.Snitch.Metrics.Models
{
    internal class Final : IMetric
    {
        private readonly double value;

        public Final(double value)
        {
            this.value = value;
        }

        public void Add(AggregatedEvent @event) =>
            throw new Exception("Final should not be modified");

        public void Add(double _) =>
            throw new Exception("Final should not be modified");

        public double[] ToAggregatedEventValues() =>
            throw new Exception("Final should not be aggregated");

        public MetricDataPoint[] ToMetricDataPoints() =>
            new[] {new MetricDataPoint(value)};

        public override string ToString() =>
            $"{nameof(value)}: {value}";
    }
}