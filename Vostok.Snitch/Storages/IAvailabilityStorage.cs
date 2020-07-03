using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Snitch.Configuration;
using Vostok.Snitch.Core.Models;
using Vostok.Snitch.Metrics.Models;

namespace Vostok.Snitch.Storages
{
    public interface IAvailabilityStorage
    {
        Task<Dictionary<TopologyKey, AvailabilityCounters>> ReadAsync(AvailabilityPeriodTrackerSettings settings);
    }
}