using System;
using JetBrains.Annotations;
using Vostok.Commons.Time;

namespace Vostok.Snitch.Configuration
{
    [PublicAPI]
    public class ConsumerSettings
    {
        public TimeSpan Lag { get; set; } = 30.Seconds();

        public TimeSpan MaximumDeltaAfterNow { get; set; } = 10.Seconds();

        public int EventsReadBatchSize { get; set; } = 50_000;

        public int EventsWriteBufferCapacityLimit { get; set; } = 32 * 1024 * 1024;

        public int? EventsLimitMetric { get; set; }
    }
}