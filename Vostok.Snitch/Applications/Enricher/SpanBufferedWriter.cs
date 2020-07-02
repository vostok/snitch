using System;
using System.Threading.Tasks;
using Vostok.Commons.Binary;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Hercules.Client.Serialization.Builders;
using Vostok.Hercules.Client.Serialization.Helpers;
using Vostok.Hercules.Consumers;
using Vostok.Tracing.Abstractions;

namespace Vostok.Snitch.Applications.Enricher
{
    internal class SpanBufferedWriter
    {
        private readonly string streamName;
        private readonly StreamBinaryWriter streamBinaryWriter;
        private readonly BinaryBufferWriter buffer;
        private volatile int eventsCount;

        public SpanBufferedWriter(string streamName, StreamBinaryWriter streamBinaryWriter)
        {
            this.streamName = streamName;
            this.streamBinaryWriter = streamBinaryWriter;
            buffer = new BinaryBufferWriter(0) {Endianness = Endianness.Big};
            buffer.Write(0);
        }

        public async Task WriteAsync()
        {
            if (eventsCount == 0)
                return;

            var segment = new ArraySegment<byte>(buffer.Buffer, 0, (int)buffer.Position);

            buffer.Reset();
            buffer.Position = 0;
            buffer.Write(eventsCount);

            await streamBinaryWriter.WriteAsync(streamName, segment, eventsCount);

            eventsCount = 0;
        }

        public void Put(
            IBinaryBufferReader sourceBuffer,
            long start,
            long end,
            long annotationsBegin,
            long annotationsEnd,
            string targetEnvironment,
            string targetService,
            string url)
        {
            var reader = new BinaryBufferReader(sourceBuffer.Buffer, sourceBuffer.Position)
            {
                Endianness = Endianness.Big
            };

            short annotationsCount;
            using (reader.JumpTo(annotationsBegin))
            {
                annotationsCount = reader.ReadInt16();
            }

            Write(sourceBuffer.Buffer, start, annotationsBegin);
            var annotationsCountPosition = buffer.Position;
            Write(sourceBuffer.Buffer, annotationsBegin, annotationsEnd);

            var newAnnotationsCount = annotationsCount;

            if (targetEnvironment != null)
            {
                Write(WellKnownAnnotations.Http.Request.TargetEnvironment, targetEnvironment);
                newAnnotationsCount++;
            }

            if (targetService != null)
            {
                Write(WellKnownAnnotations.Http.Request.TargetService, targetService);
                newAnnotationsCount++;
            }

            if (url != null)
            {
                Write(WellKnownAnnotations.Http.Request.Url, url);
                newAnnotationsCount++;
            }

            using (buffer.JumpTo(annotationsCountPosition))
                buffer.Write((ushort)newAnnotationsCount);

            Write(sourceBuffer.Buffer, annotationsEnd, end);

            eventsCount++;
        }

        private void Write(string key, string value)
        {
            buffer.WriteWithByteLength(key);
            buffer.Write(TagType.String);
            buffer.WriteWithLength(value);
        }

        private void Write(byte[] sourceBuffer, long start, long end)
        {
            var length = (int)(end - start);
            buffer.EnsureCapacity(length);

            Buffer.BlockCopy(sourceBuffer, (int)start, buffer.Buffer, (int)buffer.Position, length);

            buffer.Position += length;
        }
    }
}