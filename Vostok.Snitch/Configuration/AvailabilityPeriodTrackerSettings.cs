using System;

namespace Vostok.Snitch.Configuration
{
    public class AvailabilityPeriodTrackerSettings
    {
        public string Name { get; set; }

        public TimeSpan Period { get; set; }

        public TimeSpan BucketPeriod { get; set; }

        public TimeSpan ReportPeriod { get; set; }
    }
}