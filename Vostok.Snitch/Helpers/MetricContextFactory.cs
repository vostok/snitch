using Vostok.Configuration;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Metrics;
using Vostok.Metrics.Hercules;
using Vostok.Metrics.Senders;
using Vostok.Snitch.Configuration;

namespace Vostok.Snitch.Helpers
{
    public static class MetricContextFactory
    {
        public static (IMetricContext metricContext, StreamBinaryEventsWriter eventsWriter) Create(IVostokHostingEnvironment environment, bool noTags = false)
        {
            var settings = ConfigurationProvider.Default.Get<ProducerSettings>();

            var eventsWriter = ConsumersFactory.CreateStreamBinaryEventsWriter(environment, settings.MetricsStream);

            IMetricContext metricContext = new MetricContext(
                new MetricContextConfig(
                    new AdHocMetricEventSender(
                        metricEvent => { eventsWriter.Put(b => HerculesEventMetricBuilder.Build(metricEvent, b)); })));

            if (noTags)
                return (metricContext, eventsWriter);

            metricContext = metricContext
                .WithTag("environment", settings.MetricsEnvironment)
                .WithTag("application", "snitch")
                .WithTag("generation", settings.MetricsGeneration)
                .WithTag("grouping", "topology");

            return (metricContext, eventsWriter);
        }
    }
}