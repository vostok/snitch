using Vostok.Configuration;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Core.Classifiers;
using Vostok.Snitch.Metrics.Models;

namespace Vostok.Snitch.Metrics
{
    public class MetricsProcessorSettings
    {
        public readonly bool UseHistogram;

        public readonly ResponseCodeClassifier ResponseCodeClassifier;

        public readonly ResponseCodeClassifier AvailabilityResponseCodeClassifier;

        internal readonly StatisticsHistory History;

        public MetricsProcessorSettings(bool useHistogram)
        {
            UseHistogram = useHistogram;

            ResponseCodeClassifier = new ResponseCodeClassifier(() => ConfigurationProvider.Default.Get<ResponseCodeClassifierSettings>());

            AvailabilityResponseCodeClassifier = new ResponseCodeClassifier(() => ConfigurationProvider.Default.Get<AvailabilityResponseCodeClassifierSettings>());

            History = new StatisticsHistory(ConfigurationProvider.Default.Get<MetricsSettings>().HistoryLength);
        }

        public MetricsProcessorServiceSettings For(string service)
        {
            var settings = ConfigurationProvider.Default.Get<MetricsSettings>();
            if (settings.PerServiceSettings != null && settings.PerServiceSettings.TryGetValue(service, out var serviceSettings))
                return serviceSettings;
            return settings;
        }
    }
}