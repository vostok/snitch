using System.Collections.Generic;
using Vostok.Snitch.Core.Models;

namespace Vostok.Snitch.Metrics.Models
{
    public class Statistics
    {
        public ReplicaStatistics Global;

        public Dictionary<TopologyReplica, ReplicaStatistics> Replicas = new Dictionary<TopologyReplica, ReplicaStatistics>();
    }
}