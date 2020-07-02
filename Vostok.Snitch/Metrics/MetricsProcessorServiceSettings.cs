using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Configuration.Primitives;

namespace Vostok.Snitch.Metrics
{
    [PublicAPI]
    public class MetricsProcessorServiceSettings
    {
        public int ClientsLimit { get; set; } = 50;

        public int UrlsLimit { get; set; } = 50;

        public int PenaltyMultiplierForWarnings { get; set; }

        public int PenaltyMultiplierForErrors { get; set; } = 10;

        public double Sensitivity { get; set; } = 0.9;

        public double MinimumWeight { get; set; } = 0.05;

        public double WeightByStatusesRpsThreshold { get; set; } = 1;

        public IReadOnlyList<DataSize> RequestSizeBuckets { get; set; } = Constants.SizeBuckets;
        
        public IReadOnlyList<DataSize> ResponseSizeBuckets { get; set; } = Constants.SizeBuckets;

        public TimeSpan MetricsSmoothingTimeConstant { get; set; } = 40.Seconds();

        public TimeSpan WeightsSmoothingTimeConstant { get; set; } = 30.Seconds();
    }
}