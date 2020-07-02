using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications.ClusterSnitch
{
    [PublicAPI]
    public class ClusterSnitchSettings
    {
        [Required]
        public string SourceStream { get; set; }
    }
}