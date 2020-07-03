using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Applications.Scheduled;
using Vostok.Commons.Time;
using Vostok.Hercules.Consumers;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Metrics;
using Vostok.Metrics.Models;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Helpers;
using Vostok.Snitch.Metrics;
using Vostok.Snitch.Storages;

namespace Vostok.Snitch.Applications.ClusterAvailabilityReporter
{
    [RequiresSecretConfiguration(typeof(SnitchConsumerSecretSettings))]
    public class ClusterAvailabilityReporterApplication : VostokScheduledApplication
    {
        public override void Setup(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
        {
            var settings = environment.ConfigurationProvider.Get<AvailabilityTrackerSettings>();
            var storage = environment.HostExtensions.Get<IAvailabilityStorage>();

            foreach (var period in settings.Periods)
            {
                var (metricContext, eventsWriter) = MetricContextFactory.Create(environment);
                builder.Schedule(
                    period.Name,
#if DEBUG
                    Scheduler.AlignedPeriodical(10.Seconds()),
#else
                    Scheduler.AlignedPeriodical(period.ReportPeriod),
#endif
                    c => ReportAsync(period, storage, metricContext, eventsWriter, c));
            }
        }

        private async Task ReportAsync(AvailabilityPeriodTrackerSettings period, IAvailabilityStorage storage, IMetricContext metricContext, StreamBinaryEventsWriter eventsWriter, IScheduledActionContext actionContext)
        {
            var data = await storage.ReadAsync(period);

            foreach (var (topology, counters) in data)
            {
                var total = counters.SuccessResponses + counters.WarningResponses + counters.ErrorResponses;
                if (total == 0)
                    continue;

                var metrics = new List<(string, double)>
                {
                    ("successCount", counters.SuccessResponses),
                    ("warningCount", counters.WarningResponses),
                    ("errorCount", counters.ErrorResponses),
                    ("totalCount", total),
                    ("successFraction", 1.0 * counters.SuccessResponses / total),
                    ("warningFraction", 1.0 * counters.WarningResponses / total),
                    ("errorFraction", 1.0 * counters.ErrorResponses / total)
                };

                foreach (var (key, value) in metrics)
                {
                    metricContext.Send(
                        new MetricDataPoint(
                            value,
                            (WellKnownTagKeys.Name, topology.GetMetricName()),
                            ("type", "availability"),
                            ("period", period.Name),
                            ("subtype", key))
                        {
                            Timestamp = actionContext.Timestamp
                        });
                }
            }

            await eventsWriter.WriteAsync();
        }
    }
}