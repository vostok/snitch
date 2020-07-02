using System.Collections.Generic;
using Vostok.Commons.Time;

namespace Vostok.Snitch.Configuration
{
    public class AvailabilityTrackerSettings
    {
        public List<AvailabilityPeriodTrackerSettings> Periods { get; set; } = new List<AvailabilityPeriodTrackerSettings>
        {
            new AvailabilityPeriodTrackerSettings
            {
                Name = "daily",
                Period = 1.Days(),
                BucketPeriod = 10.Minutes(),
                ReportPeriod = 10.Minutes()
            },
            new AvailabilityPeriodTrackerSettings
            {
                Name = "monthly",
                Period = 30.Days(),
                BucketPeriod = 4.Hours(),
                ReportPeriod = 10.Minutes()
            },
            new AvailabilityPeriodTrackerSettings
            {
                Name = "quarterly",
                Period = 90.Days(),
                BucketPeriod = 12.Hours(),
                ReportPeriod = 10.Minutes()
            },
            new AvailabilityPeriodTrackerSettings
            {
                Name = "annual",
                Period = 365.Days(),
                BucketPeriod = 1.Days(),
                ReportPeriod = 10.Minutes()
            }
        };
    }
}