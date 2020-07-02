using System;
using Vostok.Metrics.Models;

namespace Vostok.Snitch.AggregatedEvents
{
    public class AggregatedEvent
    {
        public DateTimeOffset Timestamp { get; set; }

        public string TargetEnvironment { get; set; }

        public string TargetService { get; set; }

        public AggregatedEventType Type { get; set; }

        public MetricTags Key { get; set; }

        public double[] Values { get; set; }
    }
}