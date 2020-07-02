using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Metrics.Models;
using Vostok.Metrics.Primitives.Timer;
using Vostok.Snitch.AggregatedEvents;

namespace Vostok.Snitch.Metrics.Models
{
    internal class Timer : IMetric
    {
        private readonly List<double> values = new List<double>();

        public void Add(AggregatedEvent @event) =>
            throw new Exception("Histogram should be used with AggregatedEvent");

        public void Add(double value) =>
            values.Add(value);

        public MetricDataPoint[] ToMetricDataPoints()
        {
            values.Sort();

            return Constants.Quantiles
                .Select(
                    (q, i) => new MetricDataPoint(
                        Quantiles.GetQuantile(q, values, values.Count),
                        (Constants.QuantileTags[i].Key, Constants.QuantileTags[i].Value)))
                .ToArray();
        }

        public override string ToString() =>
            string.Join(", ", ToMetricDataPoints().Select(p => $"{p.Tags[0].value}:{p.Value:F1}"));

        double[] IMetric.ToAggregatedEventValues() =>
            throw new Exception("Histogram should be used with AggregatedEvent");
    }
}