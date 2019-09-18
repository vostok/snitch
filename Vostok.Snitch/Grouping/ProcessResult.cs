using System.Collections.Generic;
using Vostok.Hercules.Client.Abstractions.Models;
using Vostok.Hercules.Consumers.Helpers;
using Vostok.Metrics.Models;

namespace Vostok.Snitch.Grouping
{
    internal class ProcessResult
    {
        public StreamCoordinates FirstActiveEventCoordinates;

        public long ActiveEventsCount;

        public long ActiveWindowsCount;

        public void AddActiveWindow(Window window)
        {
            ActiveWindowsCount++;
            ActiveEventsCount += window.EventsCount;
            AddActiveCoordinates(window.FirstEventCoordinates);
        }

        public void AddProcessResult(ProcessResult other)
        {
            AddActiveCoordinates(other.FirstActiveEventCoordinates);
            ActiveEventsCount += other.ActiveEventsCount;
            ActiveWindowsCount += other.ActiveWindowsCount;
        }

        private void AddActiveCoordinates(StreamCoordinates coordinates)
        {
            if (FirstActiveEventCoordinates == null)
                FirstActiveEventCoordinates = coordinates;
            else if (coordinates != null)
                FirstActiveEventCoordinates = StreamCoordinatesMerger.MergeMin(FirstActiveEventCoordinates, coordinates);
        }
    }
}