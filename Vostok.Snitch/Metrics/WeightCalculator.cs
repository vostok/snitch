using System;
using Vostok.Snitch.Metrics.Models;

namespace Vostok.Snitch.Metrics
{
    internal static class WeightCalculator
    {
        private const int ReplicaToGlobalStdDevRatioCap = 3;

        public static double ComputeWeightByLatencies(ReplicaStatistics global, ReplicaStatistics replica, double sensitivity)
        {
            var replicaStdDev = Math.Min(replica.LatencyStdDeviation, global.LatencyStdDeviation * ReplicaToGlobalStdDevRatioCap);

            var weight = ComputeCdf(global.LatencyMean, replica.LatencyMean, replicaStdDev);

            weight = Math.Pow(weight, sensitivity);

            return weight;
        }

        public static double ComputeWeightByLatencies2(ReplicaStatistics global, ReplicaStatistics replica, double sensitivity)
        {
            // http://homework.uoregon.edu/pub/class/es202/ztest.html

            var stdDev = Math.Sqrt(global.LatencyVariance + replica.LatencyVariance);

            var weight = ComputeCdf(global.LatencyMean, replica.LatencyMean, stdDev);

            weight = Math.Pow(weight, sensitivity);

            return weight;
        }

        public static double ComputeWeightByStatuses(ReplicaStatistics global, ReplicaStatistics replica, double sensitivity)
        {
            // https://stats.stackexchange.com/a/113607

            var p1 = global.ErrorFraction;
            var n1 = global.TotalCount;

            var p2 = replica.ErrorFraction;
            var n2 = replica.TotalCount;

            var phat = (n1 * p1 + n2 * p2) / (n1 + n2);

            var stdDev = Math.Sqrt(phat * (1 - phat) * (1.0 / n1 + 1.0 / n2));

            var weight = ComputeCdf(p1, p2, stdDev);

            weight = Math.Pow(weight, sensitivity);

            return weight;
        }

        private static double ComputeCdf(double x, double mean, double stdDev)
        {
            if (stdDev <= double.Epsilon)
                return mean > x ? 0.0d : mean < x ? 1.0d : 0.5d;

            var y = (x - mean) / stdDev;

            return ComputeCdf(y);
        }

        private static double ComputeCdf(double z) =>
            1.0 / (1.0 + Math.Exp(-z * (1.5976 + 0.070566 * z * z)));
    }
}