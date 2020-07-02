using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Commons.Time;
using Vostok.Configuration.Primitives;
using Vostok.Metrics.Models;
using Vostok.Metrics.Primitives.Timer;

namespace Vostok.Snitch
{
    public static class Constants
    {
        public static readonly HistogramBuckets HistogramBuckets = CreateHistogramBuckets();

        public static readonly IReadOnlyList<DataSize> SizeBuckets = new[] {1.Megabytes()};

        public static TimeSpan Period = 10.Seconds();

        public static double[] Quantiles => new[] {0.25, 0.5, 0.75, 0.95, 0.99};

        public static MetricTag[] QuantileTags => Quantiles.Select(Vostok.Metrics.Primitives.Timer.Quantiles.QuantileTag).ToArray();

        private static HistogramBuckets CreateHistogramBuckets()
        {
            var upperBounds = new double[30];

            upperBounds[0] = 0;
            upperBounds[1] = 1;
            for (var i = 2; i < upperBounds.Length; i++)
                upperBounds[i] = upperBounds[i - 1] * 2;

            return new HistogramBuckets(upperBounds);
        }
    }
}