using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Configuration
{
    [PublicAPI]
    public class ProducerSettings
    {
        [Required]
        public string MetricsEnvironment { get; set; }

        [Required]
        public string MetricsGeneration { get; set; }

        [Required]
        public string MetricsStream { get; set; }
    }
}