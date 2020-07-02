using System.Threading.Tasks;

namespace Vostok.Snitch.Storage
{
    public interface ITopologyStatisticsWriter
    {
        Task WriteAsync();
    }
}