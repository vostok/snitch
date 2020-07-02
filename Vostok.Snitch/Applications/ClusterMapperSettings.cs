using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications
{
    [PublicAPI]
    public class ClusterMapperSettings
    {
        [Required]
        public string SourceStream { get; set; }

        [Required]
        public string TargetStream { get; set; }
    }
}