using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Configuration.Abstractions.Attributes;

namespace Vostok.Snitch.Applications.Enricher
{
    [PublicAPI]
    public class EnricherSettings
    {
        [Required]
        public string SourceStream { get; set; }

        public string ClientSpansTargetStreamName { get; set; }

        public string BigClientSpansTargetStreamName { get; set; }

        public string ClusterSpansTargetStreamName { get; set; }

        public string BigClusterSpansTargetStreamName { get; set; }

        public string ErrorsTargetStreamName { get; set; }

        public IReadOnlyCollection<string> BigServices { get; set; }
    }
}