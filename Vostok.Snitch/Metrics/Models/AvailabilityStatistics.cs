using System;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Metrics.Models
{
    public class AvailabilityStatistics
    {
        public readonly TopologyKey Topology;

        public readonly DateTimeOffset Timestamp;

        public readonly AvailabilityCounters Counters;

        public AvailabilityStatistics(
            TopologyKey topology,
            DateTimeOffset timestamp,
            long successResponses,
            long warningResponses,
            long errorResponses)
        {
            Topology = topology;

            Timestamp = timestamp;

            Counters = new AvailabilityCounters(successResponses, warningResponses, errorResponses);
        }
    }
}