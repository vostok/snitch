using System.Threading.Tasks;

namespace Vostok.Snitch.Storages
{
    public interface ITopologyStatisticsWriter
    {
        Task WriteAsync();
    }
}