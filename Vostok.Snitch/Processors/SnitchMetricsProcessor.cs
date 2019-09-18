using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch.Processors
{
    [PublicAPI]
    public class SnitchMetricsProcessor : ISnitchProcessor
    {
        [NotNull]
        private readonly SnitchMetricsProcessorSettings settings;
        private readonly ILog log;

        public SnitchMetricsProcessor([NotNull] SnitchMetricsProcessorSettings settings, Target target, ILog log)
        {
            this.settings = settings;
            this.log = log;
        }

        public void Process(DateTime timestamp, IReadOnlyList<HerculesHttpSpan> spans)
        {
            
        }
    }
}