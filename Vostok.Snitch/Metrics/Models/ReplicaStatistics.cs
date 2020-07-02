using System;

namespace Vostok.Snitch.Metrics.Models
{
    public class ReplicaStatistics
    {
        public readonly DateTimeOffset Timestamp;

        public readonly double SuccessCount;
        public readonly double WarningCount;
        public readonly double ErrorCount;
        public readonly double TotalCount;

        public readonly double SuccessLatency;
        public readonly double WarningLatency;
        public readonly double ErrorLatency;

        public readonly double SuccessSquaredLatency;
        public readonly double WarningSquaredLatency;
        public readonly double ErrorSquaredLatency;

        public readonly double LatencyMean;
        public readonly double LatencyVariance;

        public ReplicaStatistics(
            DateTimeOffset timestamp,
            double? successCount,
            double? warningCount,
            double? errorCount,
            double? successLatency,
            double? warningLatency,
            double? errorLatency,
            double? successSquaredLatency,
            double? warningSquaredLatency,
            double? errorSquaredLatency)
        {
            Timestamp = timestamp;

            SuccessCount = successCount ?? 0;
            WarningCount = warningCount ?? 0;
            ErrorCount = errorCount ?? 0;
            TotalCount = SuccessCount + WarningCount + ErrorCount;

            SuccessLatency = successLatency ?? 0;
            WarningLatency = warningLatency ?? 0;
            ErrorLatency = errorLatency ?? 0;

            SuccessSquaredLatency = successSquaredLatency ?? 0;
            WarningSquaredLatency = warningSquaredLatency ?? 0;
            ErrorSquaredLatency = errorSquaredLatency ?? 0;

            LatencyMean = (SuccessLatency + WarningLatency + ErrorLatency) / Math.Max(1, TotalCount);

            var latencySquaredMean = (SuccessSquaredLatency + WarningSquaredLatency + ErrorSquaredLatency) / Math.Max(1, TotalCount);
            LatencyVariance = latencySquaredMean - Math.Pow(LatencyMean, 2);
        }

        public ReplicaStatistics(
            DateTimeOffset timestamp,
            double? successCount,
            double? warningCount,
            double? errorCount,
            double latencyMean,
            double latencyVariance)
        {
            Timestamp = timestamp;

            SuccessCount = successCount ?? 0;
            WarningCount = warningCount ?? 0;
            ErrorCount = errorCount ?? 0;
            TotalCount = SuccessCount + WarningCount + ErrorCount;

            LatencyMean = latencyMean;
            LatencyVariance = latencyVariance;
        }

        public double SuccessFraction => SuccessCount / Math.Max(1, TotalCount);

        public double WarningFraction => WarningCount / Math.Max(1, TotalCount);

        public double ErrorFraction => ErrorCount / Math.Max(1, TotalCount);

        public double LatencyStdDeviation => Math.Sqrt(LatencyVariance);

        public override string ToString() =>
            $"{nameof(TotalCount)}: {TotalCount}, {nameof(ErrorCount)}: {ErrorCount}, {nameof(LatencyMean)}: {LatencyMean:F1}, {nameof(LatencyStdDeviation)}: {LatencyStdDeviation:F1}";
    }
}