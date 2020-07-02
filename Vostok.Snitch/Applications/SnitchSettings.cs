using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications
{
    [PublicAPI]
    public class SnitchSettings
    {
        [Required]
        public string SourceStream { get; set; }

        [Required]
        public string ZooKeeperNode { get; set; }
    }
}