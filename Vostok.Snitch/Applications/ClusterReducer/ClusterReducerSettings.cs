using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications.ClusterReducer
{
    [PublicAPI]
    public class ClusterReducerSettings
    {
        [Required]
        public string SourceStream { get; set; }

        [Required]
        public string ZooKeeperNode { get; set; }
    }
}