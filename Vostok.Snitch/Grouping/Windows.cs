using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Snitch.Processors;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch.Grouping
{
    internal class Windows
    {
        private readonly ISnitchProcessor processor;
        private readonly TimeSpan period;
        private readonly TimeSpan lag;
        private readonly List<Window> windows = new List<Window>();
        private DateTime minimumAllowedTimestamp = DateTime.MinValue;
        private DateTime maximumObservedTimestamp = DateTime.MinValue;

        public Windows(ISnitchProcessor processor, TimeSpan period, TimeSpan lag)
        {
            this.processor = processor;
            this.period = period;
            this.lag = lag;
        }

        public bool AddSpan([NotNull] HerculesHttpSpan span, [NotNull] StreamCoordinates coordinates)
        {
            if (span.UtcTimestamp < minimumAllowedTimestamp)
                return false;

            if (maximumObservedTimestamp < span.UtcTimestamp)
                maximumObservedTimestamp = span.UtcTimestamp;

            foreach (var window in windows)
            {
                if (window.AddSpan(span))
                    return true;
            }

            var newWindow = Window.Create(
                coordinates,
                span.UtcTimestamp,
                period,
                lag);

            newWindow.AddSpan(span);
            windows.Add(newWindow);

            return true;
        }

        [NotNull]
        public ProcessResult Process(bool restartPhase = false)
        {
            var result = new ProcessResult();

            for (var i = 0; i < windows.Count; i++)
            {
                var window = windows[i];

                if (window.ShouldBeClosedBefore(maximumObservedTimestamp) || window.ExistsForTooLong(restartPhase))
                {
                    windows.RemoveAt(i--);

                    if (!restartPhase)
                        processor.Process(window.End, window.GetSpans());

                    if (minimumAllowedTimestamp < window.End)
                        minimumAllowedTimestamp = window.End;
                }
                else
                {
                    result.AddActiveWindow(window);
                }
            }

            return result;
        }
    }
}