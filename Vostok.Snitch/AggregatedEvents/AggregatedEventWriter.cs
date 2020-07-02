using Vostok.Hercules.Client.Abstractions.Events;

namespace Vostok.Snitch.AggregatedEvents
{
    public static class AggregatedEventWriter
    {
        public static void Build(AggregatedEvent @event, IHerculesEventBuilder builder)
        {
            builder.SetTimestamp(@event.Timestamp);

            builder.AddValue(TagNames.TargetEnvironment, @event.TargetEnvironment);
            builder.AddValue(TagNames.TargetService, @event.TargetService);

            builder.AddValue(TagNames.Type, (byte)@event.Type);

            builder.AddVectorOfContainers(
                TagNames.Key,
                @event.Key,
                (b, pair) =>
                {
                    b.AddValue(TagNames.Key, pair.Key);
                    b.AddValue(TagNames.Value, pair.Value);
                });

            builder.AddVector(TagNames.Values, @event.Values);
        }
    }
}