using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Snitch.Helpers;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch.Grouping
{
    internal class Window
    {
        private static readonly TimeSpan MaximumAllowedPeriod = 1.Minutes();
        private static readonly TimeSpan MaximumAllowedLag = 1.Minutes();
        public readonly StreamCoordinates FirstEventCoordinates;
        public readonly DateTime Start;
        public readonly DateTime End;
        public readonly TimeSpan Period;
        public readonly TimeSpan Lag;

        private readonly List<HerculesHttpSpan> spans;

        private DateTime lastEventAdded;

        internal Window(StreamCoordinates firstEventCoordinates, DateTime start, DateTime end, TimeSpan period, TimeSpan lag)
        {
            FirstEventCoordinates = firstEventCoordinates;
            Start = start;
            End = end;
            Period = period;
            Lag = lag;
            spans = new List<HerculesHttpSpan>();
            lastEventAdded = DateTime.Now;
        }

        [NotNull]
        public static Window Create(StreamCoordinates firstEventCoordinates, DateTime timestamp, TimeSpan period, TimeSpan lag)
        {
            if (period > MaximumAllowedPeriod)
                period = MaximumAllowedPeriod;
            if (lag > MaximumAllowedLag)
                lag = MaximumAllowedLag;

            var start = timestamp.AddTicks(-timestamp.Ticks % period.Ticks);
            var result = new Window(firstEventCoordinates, start, start + period, period, lag);
            return result;
        }

        public int EventsCount => spans.Count;

        public bool AddSpan([NotNull] HerculesHttpSpan span)
        {
            if (!span.UtcTimestamp.InInterval(Start, End))
                return false;

            lastEventAdded = DateTime.Now;
            spans.Add(span);

            return true;
        }

        public bool ShouldBeClosedBefore(DateTime timestamp)
        {
            return End + Lag <= timestamp;
        }

        public bool ExistsForTooLong(bool restartPhase = false)
        {
            if (restartPhase)
            {
                lastEventAdded = DateTime.Now;
                return false;
            }

            return DateTime.Now - lastEventAdded > Period + Lag;
        }

        public IReadOnlyList<HerculesHttpSpan> GetSpans() => spans;
    }
}