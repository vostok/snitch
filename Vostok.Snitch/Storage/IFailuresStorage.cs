using System.Collections.Generic;
using System.Threading.Tasks;
using Vostok.Tracing.Hercules.Models;

namespace Vostok.Snitch.Storage
{
    public interface IFailuresStorage
    {
        Task WriteAsync(List<HerculesHttpClientSpan> spans);
    }
}