using System;
using System.Linq;
using Vostok.Metrics.Models;
using Vostok.Snitch.Events;

namespace Vostok.Snitch.Metrics.Models
{
    internal class Histogram : IMetric
    {
        private readonly double[] bucketCounters;

        public Histogram()
        {
            bucketCounters = new double[Constants.HistogramBuckets.Count];
        }

        public void Add(AggregatedEvent @event)
        {
            var n = Math.Min(@event.Values.Length, bucketCounters.Length);
            for (var i = 0; i < n; i++)
                bucketCounters[i] += @event.Values[i];
        }

        public void Add(double value) =>
            bucketCounters[Constants.HistogramBuckets.FindBucketIndex(value)]++;

        public double[] ToAggregatedEventValues() =>
            bucketCounters;

        public MetricDataPoint[] ToMetricDataPoints()
        {
            return Constants.Quantiles
                .Select(
                    (q, i) => new MetricDataPoint(
                        GetQuantile(q),
                        (Constants.QuantileTags[i].Key, Constants.QuantileTags[i].Value)))
                .ToArray();
        }

        public override string ToString() =>
            string.Join(", ", ToMetricDataPoints().Select(p => $"{p.Tags[0].value}:{p.Value:F1}"));

        private double GetQuantile(double quantile)
        {
            var totalCount = bucketCounters.Sum();
            var skip = quantile * totalCount;

            var i = 0;
            while (i + 1 < bucketCounters.Length && bucketCounters[i] < skip)
            {
                skip -= bucketCounters[i];
                i++;
            }

            var bucket = Constants.HistogramBuckets[i];
            var value = bucketCounters[i];

            if (double.IsPositiveInfinity(bucket.UpperBound))
                return bucket.LowerBound;

            if (double.IsNegativeInfinity(bucket.LowerBound))
                return bucket.UpperBound;

            var length = bucket.UpperBound - bucket.LowerBound;
            var result = bucket.LowerBound + skip / value * length;

            return result;
        }
    }
}