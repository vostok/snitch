using System.Collections.Generic;
using System.Linq;
using Vostok.Configuration;
using Vostok.Snitch.Core.Classifiers;
using Vostok.Snitch.Core.Models;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Applications.FailuresIndexer
{
    internal class TargetsFailures
    {
        private readonly TargetsFailuresSettings settings;

        private readonly ResponseCodeClassifier responseCodeClassifier;
        private readonly LatencyClassifier latencyClassifier;

        private Dictionary<TopologyKey, List<HerculesHttpClientSpan>> errors;
        private Dictionary<TopologyKey, List<HerculesHttpClientSpan>> warnings;
        private Dictionary<TopologyKey, List<HerculesHttpClientSpan>> slows;

        public TargetsFailures(TargetsFailuresSettings settings)
        {
            this.settings = settings;

            errors = new Dictionary<TopologyKey, List<HerculesHttpClientSpan>>();
            warnings = new Dictionary<TopologyKey, List<HerculesHttpClientSpan>>();
            slows = new Dictionary<TopologyKey, List<HerculesHttpClientSpan>>();

            responseCodeClassifier = new ResponseCodeClassifier(() => ConfigurationProvider.Default.Get<ResponseCodeClassifierSettings>());
            latencyClassifier = new LatencyClassifier(() => ConfigurationProvider.Default.Get<LatencyClassifierSettings>());
        }

        public List<HerculesHttpClientSpan> CollectErrors() => Collect(ref errors);
        public List<HerculesHttpClientSpan> CollectWarnings() => Collect(ref warnings);
        public List<HerculesHttpClientSpan> CollectSlows() => Collect(ref slows);

        public void Add(HerculesHttpClientSpan span)
        {
            switch (responseCodeClassifier.Classify(span.TargetService, span.ResponseCode, span.Latency))
            {
                case ResponseCodeClass.Warning:
                    Add(span, warnings);
                    break;
                case ResponseCodeClass.Error:
                    Add(span, errors);
                    break;
            }

            if (latencyClassifier.IsSlow(span.TargetService, span.Latency))
                Add(span, slows);
        }

        private List<HerculesHttpClientSpan> Collect(ref Dictionary<TopologyKey, List<HerculesHttpClientSpan>> dictionary)
        {
            var result = dictionary.SelectMany(x => x.Value).ToList();
            dictionary.Clear();
            return result;
        }

        private void Add(HerculesHttpClientSpan span, Dictionary<TopologyKey, List<HerculesHttpClientSpan>> dictionary)
        {
            var target = new TopologyKey(span.TargetEnvironment, span.TargetService);
            if (!dictionary.TryGetValue(target, out var spans))
                dictionary[target] = spans = new List<HerculesHttpClientSpan>();

            if (spans.Count >= settings.LimitPerService)
                return;

            spans.Add(span);
        }
    }
}