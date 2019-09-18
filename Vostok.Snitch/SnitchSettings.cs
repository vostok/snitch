using System;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Hercules.Client.Abstractions;
using Vostok.Hercules.Consumers;
using Vostok.Metrics;
using Vostok.Snitch.Processors;
using Vostok.Tracing.Hercules;

namespace Vostok.Snitch
{
    [PublicAPI]
    public class SnitchSettings
    {
        public SnitchSettings(
            [NotNull] string streamName,
            [NotNull] Func<Target, ISnitchProcessor> snitchProcessorsFactory,
            [NotNull] IHerculesStreamClient<HerculesHttpSpan> streamClient,
            [NotNull] IStreamCoordinatesStorage leftCoordinatesStorage,
            [NotNull] IStreamCoordinatesStorage rightCoordinatesStorage,
            [NotNull] Func<StreamShardingSettings> shardingSettingsProvider,
            [NotNull] IMetricContext metricContext)
        {
            StreamName = streamName ?? throw new ArgumentNullException(nameof(streamName));
            SnitchConsumerFactory = snitchProcessorsFactory ?? throw new ArgumentNullException(nameof(snitchProcessorsFactory));
            MetricContext = metricContext ?? throw new ArgumentNullException(nameof(metricContext));
            StreamClient = streamClient ?? throw new ArgumentNullException(nameof(streamClient));
            LeftCoordinatesStorage = leftCoordinatesStorage ?? throw new ArgumentNullException(nameof(leftCoordinatesStorage));
            RightCoordinatesStorage = rightCoordinatesStorage ?? throw new ArgumentNullException(nameof(rightCoordinatesStorage));
            ShardingSettingsProvider = shardingSettingsProvider ?? throw new ArgumentNullException(nameof(shardingSettingsProvider));
        }

        [NotNull]
        public string StreamName { get; }
        
        [NotNull]
        public Func<Target, ISnitchProcessor> SnitchConsumerFactory { get; }

        [NotNull]
        public IMetricContext MetricContext { get; }

        [NotNull]
        public IHerculesStreamClient<HerculesHttpSpan> StreamClient { get; }

        [NotNull]
        public IStreamCoordinatesStorage LeftCoordinatesStorage { get; }

        [NotNull]
        public IStreamCoordinatesStorage RightCoordinatesStorage { get; }

        [NotNull]
        public Func<StreamShardingSettings> ShardingSettingsProvider { get; }

        public int EventsReadBatchSize { get; set; } = 100_000;

        public TimeSpan EventsReadTimeout { get; set; } = TimeSpan.FromSeconds(45);

        public TimeSpan DelayOnError { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan DelayOnNoEvents { get; set; } = TimeSpan.FromSeconds(2);

        public TimeSpan Period { get; set; } = 1.Minutes();

        public TimeSpan Lag { get; set; } = 30.Seconds();

        public TimeSpan MaximumEventBeforeNow { get; set; } = 1.Days();

        public TimeSpan MaximumEventAfterNow { get; set; } = 1.Minutes();

        public TimeSpan Ttl { get; set; } = 1.Hours();
    }
}