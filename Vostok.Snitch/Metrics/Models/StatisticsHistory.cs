using System.Collections.Generic;
using System.Linq;
using Vostok.Commons.Collections;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Metrics.Models
{
    internal class StatisticsHistory
    {
        private readonly int capacity;
        private readonly Dictionary<TopologyKey, CircularBuffer<Statistics>> statisticsHistory;
        private readonly Dictionary<TopologyKey, CircularBuffer<Weights>> weightsHistory;

        public StatisticsHistory(int capacity)
        {
            this.capacity = capacity;

            statisticsHistory = new Dictionary<TopologyKey, CircularBuffer<Statistics>>();
            weightsHistory = new Dictionary<TopologyKey, CircularBuffer<Weights>>();
        }

        public void Put(TopologyKey topology, Statistics statistics)
        {
            if (!statisticsHistory.TryGetValue(topology, out var history))
                statisticsHistory[topology] = history = new CircularBuffer<Statistics>(capacity);

            history.Add(statistics);
        }

        public void Put(TopologyKey topology, Weights weights)
        {
            if (!weightsHistory.TryGetValue(topology, out var history))
                weightsHistory[topology] = history = new CircularBuffer<Weights>(capacity);

            history.Add(weights);
        }

        public Statistics GetPreviousStatistics(TopologyKey topology) =>
            statisticsHistory.TryGetValue(topology, out var buffer) ? buffer.Last : null;

        public List<Statistics> GetPreviousStatistics(TopologyKey topology, int take) =>
            statisticsHistory.TryGetValue(topology, out var buffer) ? buffer.EnumerateReverse().Take(take).ToList() : null;

        public Weights GetPreviousWeights(TopologyKey topology) =>
            weightsHistory.TryGetValue(topology, out var buffer) ? buffer.Last : null;
    }
}