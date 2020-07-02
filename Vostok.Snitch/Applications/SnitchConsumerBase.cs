using System;
using System.Threading.Tasks;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Metrics.Primitives.Gauge;
using Vostok.Snitch.Configuration;

namespace Vostok.Snitch.Applications
{
    [RequiresSecretConfiguration(typeof(SnitchSecretSettings))]
    public abstract class SnitchConsumerBase : IVostokApplication
    {
        public async Task InitializeAsync(IVostokHostingEnvironment environment)
        {
            SetupEventsLimitMetric(environment, () => environment.ConfigurationProvider.Get<ConsumerSettings>().EventsLimitMetric);

            await InitializeConsumerAsync(environment);
        }

        public Task RunAsync(IVostokHostingEnvironment environment)
        {
            return RunConsumerAsync(environment);
        }

        protected abstract Task InitializeConsumerAsync(IVostokHostingEnvironment environment);

        protected abstract Task RunConsumerAsync(IVostokHostingEnvironment environment);

        private void SetupEventsLimitMetric(IVostokHostingEnvironment environment, Func<int?> limit)
        {
            if (environment.ApplicationReplicationInfo.InstanceIndex == 0)
            {
                environment.Metrics.Application.CreateFuncGauge("events", "type")
                    .For("limit")
                    .SetValueProvider(() => limit());
            }
        }
    }
}