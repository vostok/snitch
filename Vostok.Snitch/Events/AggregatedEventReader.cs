using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Metrics.Models;

// ReSharper disable ParameterHidesMember

namespace Vostok.Snitch.Events
{
    public class AggregatedEventReader : DummyHerculesTagsBuilder, IHerculesEventBuilder<AggregatedEvent>
    {
        private static readonly DummyHerculesTagsBuilder DummyBuilder = new DummyHerculesTagsBuilder();

        private AggregatedEvent @event;

        public AggregatedEventReader(IBinaryBufferReader reader)
        {
            @event = new AggregatedEvent();
        }

        public IHerculesEventBuilder<AggregatedEvent> SetTimestamp(DateTimeOffset timestamp)
        {
            @event.Timestamp = timestamp;
            return this;
        }

        public AggregatedEvent BuildEvent() =>
            @event;

        public new IHerculesTagsBuilder AddValue(string key, string value)
        {
            switch (key)
            {
                case TagNames.TargetEnvironment:
                    @event.TargetEnvironment = value;
                    break;
                case TagNames.TargetService:
                    @event.TargetService = value;
                    break;
            }

            return this;
        }

        public new IHerculesTagsBuilder AddValue(string key, byte value)
        {
            switch (key)
            {
                case TagNames.Type:
                    @event.Type = (AggregatedEventType)value;
                    break;
            }

            return this;
        }

        public new IHerculesTagsBuilder AddVectorOfContainers(string key, IReadOnlyList<Action<IHerculesTagsBuilder>> valueBuilders)
        {
            if (key == TagNames.Key)
            {
                AddKey(valueBuilders);
            }
            else
            {
                foreach (var valueBuilder in valueBuilders)
                {
                    valueBuilder(DummyBuilder);
                }
            }

            return this;
        }

        public new IHerculesTagsBuilder AddVector(string key, IReadOnlyList<double> values)
        {
            if (key == TagNames.Values)
            {
                @event.Values = values.ToArray();
            }

            return this;
        }

        private void AddKey(IReadOnlyList<Action<IHerculesTagsBuilder>> valueBuilders)
        {
            var list = new MetricTag[valueBuilders.Count];
            var keyBuilder = new KeyBuilder();

            for (var i = 0; i < valueBuilders.Count; i++)
            {
                valueBuilders[i](keyBuilder);
                list[i] = keyBuilder.Build();
            }

            @event.Key = new MetricTags(list);
        }

        internal class KeyBuilder : DummyHerculesTagsBuilder, IHerculesTagsBuilder
        {
            private string key;
            private string value;

            public MetricTag Build()
            {
                return new MetricTag(key, value);
            }

            public new IHerculesTagsBuilder AddValue(string key, string value)
            {
                switch (key)
                {
                    case TagNames.Key:
                        this.key = value;
                        break;
                    case TagNames.Value:
                        this.value = value;
                        break;
                }

                return this;
            }
        }
    }
}