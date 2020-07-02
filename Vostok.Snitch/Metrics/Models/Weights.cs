using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Metrics.Models
{
    public class Weights : Dictionary<TopologyReplica, ReplicaWeight>
    {
        public void Sanitize()
        {
            if (Count == 0)
                return;

            var replicasWithNaN = this
                .Where(pair => double.IsNaN(pair.Value.Value))
                .Select(pair => pair.Key)
                .ToList();

            var replicasWithInfiniteWeight = this
                .Where(pair => double.IsPositiveInfinity(pair.Value.Value))
                .Select(pair => pair.Key)
                .ToList();

            var replicasWithNegativeWeight = this
                .Where(pair => pair.Value.Value < 0d)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var replica in replicasWithNaN.Concat(replicasWithNegativeWeight))
            {
                this[replica] = this[replica].WithValue(0);
            }

            foreach (var replica in replicasWithInfiniteWeight)
            {
                this[replica] = this[replica].WithValue(double.MaxValue);
            }
        }

        public void NormalizeAndEnforceLimits(double minimumWeight)
        {
            Normalize();
            EnforceLimits(minimumWeight);
        }

        private void Normalize()
        {
            if (Count == 0)
                return;

            var maximumWeight = Values.Max(w => w.Value);

            if (Math.Abs(maximumWeight) > double.Epsilon)
            {
                foreach (var replica in Keys.ToList())
                {
                    this[replica] = this[replica].WithValue(this[replica].Value / maximumWeight);
                }
            }
        }

        private void EnforceLimits(double minimumWeight)
        {
            if (Count == 0)
                return;

            var replicasToBoost = this
                .Where(pair => pair.Value.Value < minimumWeight)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var replica in replicasToBoost)
            {
                this[replica] = this[replica].WithValue(minimumWeight);
            }
        }
    }
}