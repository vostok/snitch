using System;
using Vostok.Commons.Time;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Snitch.Core.Topologies;
using Vostok.Tracing.Hercules.Helpers;

namespace Vostok.Snitch.Applications.Enricher
{
    internal class SpanReaderWriter : DummyHerculesTagsBuilder, IHerculesEventBuilder<SpanProcessResult>
    {
        private static readonly DummyHerculesTagsBuilder DummyBuilder = new DummyHerculesTagsBuilder();

        private readonly CompositeSpanBinaryWriter writer;
        private readonly IBinaryBufferReader reader;
        private readonly TopologiesResolver topologiesResolver;

        private readonly long spanBeginPosition;

        private SpanAnnotationsReader annotationsReader;
        private long annotationsBeginPosition;
        private long annotationsEndPosition;
        private long beginTimestamp;
        private long endTimestamp;

        public SpanReaderWriter(TopologiesResolver topologiesResolver, IBinaryBufferReader reader, CompositeSpanBinaryWriter writer)
        {
            this.topologiesResolver = topologiesResolver;
            this.reader = reader;
            this.writer = writer;

            spanBeginPosition = reader.Position;
        }

        public new IHerculesTagsBuilder AddValue(string key, long value)
        {
            switch (key)
            {
                case TagNames.BeginTimestampUtc:
                    beginTimestamp = value;
                    break;
                case TagNames.EndTimestampUtc:
                    endTimestamp = value;
                    break;
            }

            return this;
        }

        public new IHerculesTagsBuilder AddContainer(string key, Action<IHerculesTagsBuilder> valueBuilder)
        {
            if (key == TagNames.Annotations)
            {
                AddAnnotations(valueBuilder);
            }
            else
            {
                valueBuilder(DummyBuilder);
            }

            return this;
        }

        public SpanProcessResult BuildEvent()
        {
            var result = new SpanProcessResult();

            if (annotationsReader == null)
                return result;
            if (annotationsReader.Url == null || annotationsReader.Code == null)
                return result;
            if (!Uri.TryCreate(annotationsReader.Url, UriKind.RelativeOrAbsolute, out var parsedUrl))
                return result;

            foreach (var targetTopology in topologiesResolver.Resolve(parsedUrl, annotationsReader.TargetEnvironment, annotationsReader.TargetService))
            {
                result.WritesCount++;
                writer.WriteSpan(
                    annotationsReader.Kind,
                    reader,
                    spanBeginPosition,
                    reader.Position,
                    annotationsBeginPosition,
                    annotationsEndPosition,
                    targetTopology.Environment,
                    targetTopology.Service,
                    parsedUrl,
                    annotationsReader.Code.Value,
                    (endTimestamp - beginTimestamp).Ticks());
            }

            return result;
        }

        protected void AddAnnotations(Action<IHerculesTagsBuilder> valueBuilder)
        {
            annotationsBeginPosition = reader.Position;
            annotationsReader = new SpanAnnotationsReader();
            valueBuilder(annotationsReader);
            annotationsEndPosition = reader.Position;
        }

        IHerculesEventBuilder<SpanProcessResult> IHerculesEventBuilder<SpanProcessResult>.SetTimestamp(DateTimeOffset timestamp) => this;
    }
}