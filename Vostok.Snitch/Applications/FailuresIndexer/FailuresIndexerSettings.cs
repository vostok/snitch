using System;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications.FailuresIndexer
{
    [PublicAPI]
    public class FailuresIndexerSettings
    {
        [Required]
        public string SourceStream { get; set; }
        
        public TimeSpan FlushPeriod { get; set; } = 10.Seconds();
    }
}