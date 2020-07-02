using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Snitch.Metrics;

namespace Vostok.Snitch.Configuration
{
    [PublicAPI]
    public class MetricsSettings : MetricsProcessorServiceSettings
    {
        public int WarmupIterations { get; set; } = 3;

        public int HistoryLength { get; set; } = 10;

        public int AccumulateLength { get; set; } = 4;

        [CanBeNull]
        public Dictionary<string, MetricsProcessorServiceSettings> PerServiceSettings { get; set; }
    }
}