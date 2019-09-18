using System;

namespace Vostok.Snitch.Helpers
{
    internal static class DateTimeExtensions
    {
        public static bool InInterval(this DateTime timestamp, DateTime start, DateTime end)
        {
            return start <= timestamp && timestamp < end;
        }
    }
}