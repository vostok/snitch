using Vostok.Metrics.Models;
using Vostok.Snitch.Core.Helpers;

namespace Vostok.Snitch.Metrics
{
    internal static class MetricsTags
    {
        public static readonly MetricsTagsCache Rps = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "rpsTotal"));

        public static readonly MetricsTagsCache RpsPerReplica = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "rpsPerReplica")
                .Append("replica", Escape(replica)));

        // Note(kungurtsev): unescaped name needed for saving weights to storage.
        public static readonly MetricsTagsCache RpsPerUnescapedReplica = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "rpsPerUnescapedReplica")
                .Append("replica", replica));

        public static readonly MetricsTagsCache RpsPerResponseCode = new MetricsTagsCache(
            responseCode => new MetricTags()
                .Append("type", "rpsPerResponseCode")
                .Append("responseCode", responseCode));

        public static readonly MetricsTagsCache RpsPerStatus = new MetricsTagsCache(
            status => new MetricTags()
                .Append("type", "rpsPerStatus")
                .Append("status", status));

        public static readonly MetricsTagsCache RpsPerStrategy = new MetricsTagsCache(
            strategy => new MetricTags()
                .Append("type", "rpsPerStrategy")
                .Append("strategy", strategy));

        public static readonly MetricsTagsCache RpsPerUrl = new MetricsTagsCache(
            url => new MetricTags()
                .Append("type", "rpsPerNormalizedUrl")
                .Append("url", Escape(url)));

        public static readonly MetricsTagsCache RpsPerClient = new MetricsTagsCache(
            client => new MetricTags()
                .Append("type", "rpsPerClientIdentity")
                .Append("client", Escape(client)));

        public static readonly MetricsTagsCache RpsPerResponseCodeAndClient = new MetricsTagsCache(
            (responseCode, client) => new MetricTags()
                .Append("type", "rpsPerResponseCodeAndClientIdentity")
                .Append("responseCode", responseCode)
                .Append("client", Escape(client)));

        public static readonly MetricsTagsCache RpsPerStatusAndClient = new MetricsTagsCache(
            (status, client) => new MetricTags()
                .Append("type", "rpsPerStatusAndClientIdentity")
                .Append("status", status)
                .Append("client", Escape(client)));

        public static readonly MetricsTagsCache RpsPerResponseCodeAndReplica = new MetricsTagsCache(
            (responseCode, replica) =>
                new MetricTags()
                    .Append("type", "rpsPerResponseCodeAndReplica")
                    .Append("responseCode", responseCode)
                    .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache RpsPerResponseCodeAndUrl = new MetricsTagsCache(
            (responseCode, url) => new MetricTags()
                .Append("type", "rpsPerResponseCodeAndNormalizedUrl")
                .Append("responseCode", responseCode)
                .Append("url", Escape(url)));

        public static readonly MetricsTagsCache RpsPerClientAndUrl = new MetricsTagsCache(
            (client, url) => new MetricTags()
                .Append("type", "rpsPerClientIdentityAndNormalizedUrl")
                .Append("client", Escape(client))
                .Append("url", Escape(url)));

        public static readonly MetricsTagsCache RequestTrafficPerDatacenterLink = new MetricsTagsCache(
            (from, to) => new MetricTags()
                .Append("type", "traffic")
                .Append("subtype", "requestDc")
                .Append("from", Escape(from))
                .Append("to", Escape(to)));

        public static readonly MetricsTagsCache ResponseTrafficPerDatacenterLink = new MetricsTagsCache(
            (from, to) => new MetricTags()
                .Append("type", "traffic")
                .Append("subtype", "responseDc")
                .Append("from", Escape(from))
                .Append("to", Escape(to)));

        public static readonly MetricsTagsCache RpsPerDatacenterLink = new MetricsTagsCache(
            (from, to) => new MetricTags()
                .Append("type", "traffic")
                .Append("subtype", "rpsDc")
                .Append("from", Escape(from))
                .Append("to", Escape(to)));

        public static readonly MetricsTagsCache Latency = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyTotal"));

        public static readonly MetricsTagsCache LatencyPerReplica = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyPerReplica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache LatencyPerClient = new MetricsTagsCache(
            client => new MetricTags()
                .Append("type", "latencyPerClientIdentity")
                .Append("client", Escape(client)));

        public static readonly MetricsTagsCache LatencyPerUrl = new MetricsTagsCache(
            url => new MetricTags()
                .Append("type", "latencyPerNormalizedUrl")
                .Append("url", Escape(url)));

        public static readonly MetricsTagsCache LatencyPerUrlAndRequestSize = new MetricsTagsCache(
            (url, size) => new MetricTags()
                .Append("type", "latencyPerUrlAndRequestSize")
                .Append("url", Escape(url))
                .Append("size", size));

        public static readonly MetricsTagsCache LatencyPerUrlAndResponseSize = new MetricsTagsCache(
            (url, size) => new MetricTags()
                .Append("type", "latencyPerUrlAndResponseSize")
                .Append("url", Escape(url))
                .Append("size", size));

        public static readonly MetricsTagsCache RequestSize = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "requestSizeTotal"));

        public static readonly MetricsTagsCache ResponseSize = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "responseSizeTotal"));

        public static readonly MetricsTagsCache RequestSizePerClient = new MetricsTagsCache(
            client => new MetricTags()
                .Append("type", "requestSizePerClientIdentity")
                .Append("client", Escape(client)));

        public static readonly MetricsTagsCache ResponseSizePerClient = new MetricsTagsCache(
            client => new MetricTags()
                .Append("type", "responseSizePerClientIdentity")
                .Append("client", Escape(client)));

        public static readonly MetricsTagsCache RequestSizePerUrl = new MetricsTagsCache(
            url => new MetricTags()
                .Append("type", "requestSizePerNormalizedUrl")
                .Append("url", Escape(url)));

        public static readonly MetricsTagsCache ResponseSizePerUrl = new MetricsTagsCache(
            url => new MetricTags()
                .Append("type", "responseSizePerNormalizedUrl")
                .Append("url", Escape(url)));

        public static readonly MetricsTagsCache InstantLatencyMean = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyMean")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaLatencyMean = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyMean")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache PenalizedLatencyMean = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyMean")
                .Append("evolution", "penalized")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache PenalizedReplicaLatencyMean = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyMean")
                .Append("evolution", "penalized")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache SmoothedLatencyMean = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyMean")
                .Append("evolution", "smoothed")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache SmoothedReplicaLatencyMean = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyMean")
                .Append("evolution", "smoothed")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantLatencyStdDeviation = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyStdDeviation")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaLatencyStdDeviation = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyStdDeviation")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache PenalizedLatencyStdDeviation = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyStdDeviation")
                .Append("evolution", "penalized")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache PenalizedReplicaLatencyStdDeviation = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyStdDeviation")
                .Append("evolution", "penalized")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache SmoothedLatencyStdDeviation = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyStdDeviation")
                .Append("evolution", "smoothed")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache SmoothedReplicaLatencyStdDeviation = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyStdDeviation")
                .Append("evolution", "smoothed")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantSuccessResponses = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "successResponses")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache AvailabilitySuccessResponses = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "successResponses")
                .Append("evolution", "availability")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaSuccessResponses = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "successResponses")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantSuccessFraction = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "successFraction")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantSuccessLatency = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencySuccess")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaSuccessLatency = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencySuccess")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantSuccessSquaredLatency = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencySquaredSuccess")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaSuccessSquaredLatency = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencySquaredSuccess")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantWarningResponses = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "warningResponses")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache AvailabilityWarningResponses = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "warningResponses")
                .Append("evolution", "availability")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaWarningResponses = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "warningResponses")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantWarningFraction = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "warningFraction")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantWarningLatency = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyWarning")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaWarningLatency = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyWarning")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantWarningSquaredLatency = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencySquaredWarning")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaWarningSquaredLatency = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencySquaredWarning")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantErrorResponses = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "errorResponses")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache AvailabilityErrorResponses = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "errorResponses")
                .Append("evolution", "availability")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaErrorResponses = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "errorResponses")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantErrorFraction = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "errorFraction")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantErrorLatency = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencyError")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaErrorLatency = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencyError")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantErrorSquaredLatency = new MetricsTagsCache(
            () => new MetricTags()
                .Append("type", "latencySquaredError")
                .Append("evolution", "instant")
                .Append("aggregation", "global"));

        public static readonly MetricsTagsCache InstantReplicaErrorSquaredLatency = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "latencySquaredError")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantReplicaWeight = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "weights")
                .Append("evolution", "instant")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache InstantReplicaWeight2 = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "weights")
                .Append("evolution", "instant2")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        public static readonly MetricsTagsCache SmoothedReplicaWeight = new MetricsTagsCache(
            replica => new MetricTags()
                .Append("type", "weights")
                .Append("evolution", "smoothed")
                .Append("aggregation", "replica")
                .Append("replica", Escape(replica)));

        private static string Escape(string name) =>
            name == null ? "unknown" : NamesHelper.Escape(name);
    }
}