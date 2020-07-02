using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics.Models;

namespace Vostok.Snitch.Metrics
{
    public class TopologyStatisticsCollector
    {
        private Dictionary<TopologyKey, Statistics> instant;
        private Dictionary<TopologyKey, Statistics> smoothed;
        private Dictionary<TopologyKey, Statistics> accumulated;
        private Dictionary<TopologyKey, Weights> weights;
        private List<AvailabilityStatistics> availability;

        public TopologyStatisticsCollector()
        {
            instant = new Dictionary<TopologyKey, Statistics>();
            smoothed = new Dictionary<TopologyKey, Statistics>();
            accumulated = new Dictionary<TopologyKey, Statistics>();
            weights = new Dictionary<TopologyKey, Weights>();
            availability = new List<AvailabilityStatistics>();
        }

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        internal void Put(TopologyKey key, Statistics instant, Statistics smoothed, Statistics accumulated, Weights weights)
        {
            this.instant[key] = instant;
            this.smoothed[key] = smoothed;
            this.accumulated[key] = accumulated;
            this.weights[key] = weights;
        }

        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        internal void Put(AvailabilityStatistics availability)
        {
            this.availability.Add(availability);
        }

        public Dictionary<TopologyKey, Statistics> CollectInstant() => Collect(ref instant);
        public Dictionary<TopologyKey, Statistics> CollectSmoothed() => Collect(ref smoothed);
        public Dictionary<TopologyKey, Statistics> CollectAccumulated() => Collect(ref accumulated);
        public Dictionary<TopologyKey, Weights> CollectWeights() => Collect(ref weights);
        public List<AvailabilityStatistics> CollectAvailability() => Collect(ref availability);

        private List<AvailabilityStatistics> Collect(ref List<AvailabilityStatistics> list)
        {
            var result = list;
            list = new List<AvailabilityStatistics>();
            return result;
        }

        private Dictionary<TopologyKey, Statistics> Collect(ref Dictionary<TopologyKey, Statistics> dictionary)
        {
            var result = dictionary;
            dictionary = new Dictionary<TopologyKey, Statistics>();
            return result;
        }

        private Dictionary<TopologyKey, Weights> Collect(ref Dictionary<TopologyKey, Weights> dictionary)
        {
            var result = dictionary;
            dictionary = new Dictionary<TopologyKey, Weights>();
            return result;
        }
    }
}