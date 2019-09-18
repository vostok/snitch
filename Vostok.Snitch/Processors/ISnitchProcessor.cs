using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch.Processors
{
    [PublicAPI]
    public interface ISnitchProcessor
    {
        void Process(DateTime timestamp, IReadOnlyList<HerculesHttpSpan> spans);
    }
}