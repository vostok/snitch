namespace Vostok.Snitch.Metrics.Models
{
    public class AvailabilityCounters
    {
        public readonly long SuccessResponses;
        public readonly long WarningResponses;
        public readonly long ErrorResponses;

        public static readonly AvailabilityCounters Empty = new AvailabilityCounters(0, 0, 0);

        public AvailabilityCounters(long successResponses, long warningResponses, long errorResponses)
        {
            SuccessResponses = successResponses;
            WarningResponses = warningResponses;
            ErrorResponses = errorResponses;
        }

        public static AvailabilityCounters operator + (AvailabilityCounters left, AvailabilityCounters right) =>
            new AvailabilityCounters(
                left.SuccessResponses + right.SuccessResponses,
                left.WarningResponses + right.WarningResponses,
                left.ErrorResponses + right.ErrorResponses);
    }
}