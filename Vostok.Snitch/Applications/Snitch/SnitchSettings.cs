using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications.Snitch
{
    [PublicAPI]
    public class SnitchSettings
    {
        [Required]
        public string SourceStream { get; set; }
    }
}