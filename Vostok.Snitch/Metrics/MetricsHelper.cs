using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Configuration.Primitives;
using Vostok.Snitch.Core.Helpers;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics.Models;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Metrics
{
    public static class MetricsHelper
    {
        public static TopologyReplica GetTopologyReplica(this HerculesHttpClientSpan span) =>
            new TopologyReplica(span.RequestUrl.Host, span.RequestUrl.Port, string.Empty);

        public static string GetMetricName(this TopologyKey topologyKey) =>
            NamesHelper.Escape(
                topologyKey.Environment == TopologyKey.DefaultEnvironment
                    ? topologyKey.Service
                    : $"{topologyKey.Service}-{topologyKey.Environment}");

        public static double Scale(double value) =>
            value / Constants.Period.TotalSeconds;

        public static string GetSizeBucket(long size, IReadOnlyList<DataSize> upperBounds)
        {
            if (upperBounds == null || !upperBounds.Any())
                return null;

            var pointer = 0;

            while (pointer < upperBounds.Count && upperBounds[pointer].Bytes < size)
                pointer++;

            var lowerBound = pointer == 0 ? "-inf" : upperBounds[pointer - 1].ToString();
            var upperBound = pointer == upperBounds.Count ? "inf" : upperBounds[pointer].ToString();

            return $"{lowerBound} - {upperBound}";
        }

        public static string FormatUrl(this HerculesHttpSpan span)
        {
            if (span.RequestUrl == null)
                return null;

            return span.RequestUrl.IsAbsoluteUri 
                ? $"{span.RequestMethod} {span.RequestUrl.AbsolutePath.TrimStart('/')}" 
                : $"{span.RequestMethod} {span.RequestUrl}";
        }

        public static Statistics PenalizeWarning(this Statistics statistics, double penalty) =>
            new Statistics
            {
                Global = statistics.Global.PenalizeWarning(penalty),
                Replicas = statistics.Replicas.ToDictionary(p => p.Key, p => p.Value.PenalizeWarning(penalty))
            };

        public static Statistics PenalizeError(this Statistics statistics, double penalty) =>
            new Statistics
            {
                Global = statistics.Global.PenalizeError(penalty),
                Replicas = statistics.Replicas.ToDictionary(p => p.Key, p => p.Value.PenalizeError(penalty))
            };

        public static Statistics SmoothLatencyAggregates(this Statistics current, Statistics previous, TimeSpan timeConstant) =>
            new Statistics
            {
                Global = current.Global.SmoothLatencyAggregates(previous?.Global, timeConstant),
                Replicas = current.Replicas.ToDictionary(
                    p => p.Key,
                    p =>
                        p.Value.SmoothLatencyAggregates(previous?.Replicas.GetValueOrNull(p.Key), timeConstant))
            };

        public static Statistics SmoothAggregates(this Statistics current, Statistics previous, TimeSpan timeConstant) =>
            new Statistics
            {
                Global = current.Global.SmoothAggregates(previous?.Global, timeConstant),
                Replicas = current.Replicas.ToDictionary(
                    p => p.Key,
                    p =>
                        p.Value.SmoothAggregates(previous?.Replicas.GetValueOrNull(p.Key), timeConstant))
            };

        public static ReplicaStatistics SmoothLatencyAggregates(this ReplicaStatistics current, ReplicaStatistics previous, TimeSpan timeConstant) =>
            new ReplicaStatistics(
                current.Timestamp,
                current.SuccessCount,
                current.WarningCount,
                current.ErrorCount,
                previous == null ? current.LatencyMean : SmoothValue(current.LatencyMean, previous.LatencyMean, current.Timestamp, previous.Timestamp, timeConstant),
                previous == null ? current.LatencyVariance : SmoothValue(current.LatencyVariance, previous.LatencyVariance, current.Timestamp, previous.Timestamp, timeConstant));

        public static ReplicaStatistics SmoothAggregates(this ReplicaStatistics current, ReplicaStatistics previous, TimeSpan timeConstant) =>
            new ReplicaStatistics(
                current.Timestamp,
                previous == null ? current.SuccessCount : SmoothValue(current.SuccessCount, previous.SuccessCount, current.Timestamp, previous.Timestamp, timeConstant),
                previous == null ? current.WarningCount : SmoothValue(current.WarningCount, previous.WarningCount, current.Timestamp, previous.Timestamp, timeConstant),
                previous == null ? current.ErrorCount : SmoothValue(current.ErrorCount, previous.ErrorCount, current.Timestamp, previous.Timestamp, timeConstant),
                previous == null ? current.LatencyMean : SmoothValue(current.LatencyMean, previous.LatencyMean, current.Timestamp, previous.Timestamp, timeConstant),
                previous == null ? current.LatencyVariance : SmoothValue(current.LatencyVariance, previous.LatencyVariance, current.Timestamp, previous.Timestamp, timeConstant));

        public static Weights Smooth(this Weights current, Weights previous, TimeSpan timeConstant)
        {
            var result = new Weights();
            foreach (var pair in current)
            {
                var p = previous.GetValueOrNull(pair.Key)
                        ?? new ReplicaWeight(pair.Value.Timestamp - Constants.Period, 1);

                result[pair.Key] = pair.Value.WithValue(SmoothValue(pair.Value.Value, p.Value, pair.Value.Timestamp, p.Timestamp, timeConstant));
            }

            return result;
        }

        public static Statistics UnionWith(this Statistics current, List<Statistics> previous) =>
            new Statistics
            {
                Global = current.Global.UnionWith(previous?.Select(p => p?.Global).ToList()),
                Replicas = current.Replicas.ToDictionary(
                    p => p.Key,
                    p => p.Value.UnionWith(previous?.Select(pp => pp?.Replicas.GetValueOrNull(p.Key)).ToList()))
            };

        private static ReplicaStatistics PenalizeWarning(this ReplicaStatistics statistics, double penalty) =>
            new ReplicaStatistics(
                statistics.Timestamp,
                statistics.SuccessCount,
                statistics.WarningCount,
                statistics.ErrorCount,
                statistics.SuccessLatency,
                PenalizeLatency(statistics.WarningLatency, statistics.WarningCount, penalty),
                statistics.ErrorLatency,
                statistics.SuccessSquaredLatency,
                PenalizeSquaredLatency(statistics.WarningLatency, statistics.WarningSquaredLatency, statistics.WarningCount, penalty),
                statistics.ErrorSquaredLatency);

        private static ReplicaStatistics PenalizeError(this ReplicaStatistics statistics, double penalty) =>
            new ReplicaStatistics(
                statistics.Timestamp,
                statistics.SuccessCount,
                statistics.WarningCount,
                statistics.ErrorCount,
                statistics.SuccessLatency,
                statistics.WarningLatency,
                PenalizeLatency(statistics.ErrorLatency, statistics.ErrorCount, penalty),
                statistics.SuccessSquaredLatency,
                statistics.WarningSquaredLatency,
                PenalizeSquaredLatency(statistics.ErrorLatency, statistics.ErrorSquaredLatency, statistics.ErrorCount, penalty));

        private static ReplicaStatistics UnionWith(this ReplicaStatistics current, List<ReplicaStatistics> previous) =>
            new ReplicaStatistics(
                current.Timestamp,
                current.SuccessCount + (previous?.Sum(p => p?.SuccessCount ?? 0) ?? 0),
                current.WarningCount + (previous?.Sum(p => p?.WarningCount ?? 0) ?? 0),
                current.ErrorCount + (previous?.Sum(p => p?.ErrorCount ?? 0) ?? 0),
                current.LatencyMean,
                current.LatencyVariance);

        private static double PenalizeLatency(double latency, double count, double penalty) =>
            latency + count * penalty;

        private static double PenalizeSquaredLatency(double latency, double squaredLatency, double count, double penalty) =>
            squaredLatency + 2 * penalty * latency + penalty * penalty * count;

        private static double SmoothValue(
            double currentValue,
            double previousValue,
            DateTimeOffset currentTimestamp,
            DateTimeOffset previousTimestamp,
            TimeSpan timeConstant)
        {
            var timeDifference = (currentTimestamp - previousTimestamp).TotalMilliseconds;

            var alpha = 1.0 - Math.Exp(-timeDifference / timeConstant.TotalMilliseconds);

            return alpha * currentValue + (1 - alpha) * previousValue;
        }
    }
}