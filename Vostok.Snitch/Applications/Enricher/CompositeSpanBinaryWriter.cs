using System;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Configuration;
using Vostok.Hercules.Client.Abstractions.Events;
using Vostok.Hercules.Consumers;
using Vostok.Snitch.Core;
using Vostok.Snitch.Core.Classifiers;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Core.Topologies;
using Vostok.Tracing.Abstractions;

namespace Vostok.Snitch.Applications.Enricher
{
    internal class CompositeSpanBinaryWriter
    {
        private readonly ResponseCodeClassifier responseCodeClassifier;
        private readonly LatencyClassifier latencyClassifier;
        private readonly UrlNormalizer urlNormalizer;
        private readonly TopologiesResolver topologiesResolver;
        private readonly SpanBufferedWriter clientSpansWriter;
        private readonly SpanBufferedWriter bigClientSpansWriter;
        private readonly SpanBufferedWriter clusterSpansWriter;
        private readonly SpanBufferedWriter bigClusterSpansWriter;
        private readonly SpanBufferedWriter errorsSpansWriter;

        public CompositeSpanBinaryWriter(
            StreamBinaryWriter writer,
            ResponseCodeClassifier responseCodeClassifier,
            LatencyClassifier latencyClassifier,
            UrlNormalizer urlNormalizer,
            TopologiesResolver topologiesResolver)
        {
            SpanBufferedWriter CreateSpansWriter(string streamName) =>
                streamName == null ? null : new SpanBufferedWriter(streamName, writer);

            this.responseCodeClassifier = responseCodeClassifier;
            this.latencyClassifier = latencyClassifier;
            this.urlNormalizer = urlNormalizer;
            this.topologiesResolver = topologiesResolver;

            var settings = ConfigurationProvider.Default.Get<EnricherSettings>();

            clientSpansWriter = CreateSpansWriter(settings.ClientSpansTargetStreamName);
            bigClientSpansWriter = CreateSpansWriter(settings.BigClientSpansTargetStreamName);
            clusterSpansWriter = CreateSpansWriter(settings.ClusterSpansTargetStreamName);
            bigClusterSpansWriter = CreateSpansWriter(settings.BigClusterSpansTargetStreamName);
            errorsSpansWriter = CreateSpansWriter(settings.ErrorsTargetStreamName);
        }

        public Task WriteAsync() =>
            Task.WhenAll(
                clientSpansWriter?.WriteAsync() ?? Task.CompletedTask,
                bigClientSpansWriter?.WriteAsync() ?? Task.CompletedTask,
                clusterSpansWriter?.WriteAsync() ?? Task.CompletedTask,
                bigClusterSpansWriter?.WriteAsync() ?? Task.CompletedTask,
                errorsSpansWriter?.WriteAsync() ?? Task.CompletedTask);

        public void WriteSpan(
            string kind,
            IBinaryBufferReader sourceBuffer,
            long start,
            long end,
            long annotationsBegin,
            long annotationsEnd,
            string targetEnvironment,
            string targetService,
            Uri url,
            ResponseCode code,
            TimeSpan latency)
        {
            ChooseWriter(kind, targetService)
                ?.Put(
                    sourceBuffer,
                    start,
                    end,
                    annotationsBegin,
                    annotationsEnd,
                    targetEnvironment,
                    targetService,
                    FixUrl(targetService, url, true));

            if (kind == WellKnownSpanKinds.HttpRequest.Client &&
                (responseCodeClassifier.Classify(targetService, code, latency) != ResponseCodeClass.Success
                 || latencyClassifier.IsSlow(targetService, latency)))
            {
                errorsSpansWriter
                    .Put(
                        sourceBuffer,
                        start,
                        end,
                        annotationsBegin,
                        annotationsEnd,
                        targetEnvironment,
                        targetService,
                        FixUrl(targetService, url, false));
            }
        }

        private static bool IsBigService(string targetService)
        {
            var bigServices = ConfigurationProvider.Default.Get<EnricherSettings>().BigServices;

            return bigServices?.Contains(targetService) == true;
        }

        private SpanBufferedWriter ChooseWriter(string kind, string targetService)
        {
            switch (kind)
            {
                case WellKnownSpanKinds.HttpRequest.Client:
                    return IsBigService(targetService) ? bigClientSpansWriter : clientSpansWriter;
                case WellKnownSpanKinds.HttpRequest.Cluster:
                    return IsBigService(targetService) ? bigClusterSpansWriter : clusterSpansWriter;
            }

            return null;
        }

        private string FixUrl(string targetService, Uri url, bool normalize)
        {
            if (!url.IsAbsoluteUri)
                return normalize ? urlNormalizer.NormalizePath(targetService, url) : url.ToString();

            var result = new UriBuilder(url)
            {
                Host = topologiesResolver.ResolveHost(url.Host)
            };

            if (normalize)
                result.Path = urlNormalizer.NormalizePath(targetService, url);

            return result.ToString();
        }
    }
}