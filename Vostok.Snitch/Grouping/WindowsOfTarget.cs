using System;
using JetBrains.Annotations;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Logging.Abstractions;
using Vostok.Snitch.Helpers;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch.Grouping
{
    internal class WindowsOfTarget
    {
        public DateTime LastEventAdded;
        private readonly Target target;
        private readonly SnitchSettings settings;
        private readonly ILog log;
        private readonly Windows windows;

        public WindowsOfTarget(Target target, SnitchSettings settings, ILog log)
        {
            this.target = target;
            this.settings = settings;
            this.log = log;
            windows = new Windows(settings.SnitchConsumerFactory(target), settings.Period, settings.Lag);

            LastEventAdded = DateTime.UtcNow;
        }

        public bool AddSpan([NotNull] HerculesHttpSpan span, [NotNull] StreamCoordinates coordinates)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (!span.UtcTimestamp.InInterval(now - settings.MaximumEventBeforeNow, now + settings.MaximumEventAfterNow))
                    return false;

                if (windows.AddSpan(span, coordinates))
                {
                    LastEventAdded = now;
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to add span {Span}.", span);
                return false;
            }
        }

        [NotNull]
        public ProcessResult Process(bool restartPhase = false)
        {
            try
            {
                return windows.Process(restartPhase);
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to process {Target} target.", target);
                return new ProcessResult();
            }
        }
    }
}