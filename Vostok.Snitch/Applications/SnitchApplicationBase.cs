using System.Threading.Tasks;
using Vostok.Hosting.Abstractions;
using Vostok.Hosting.Abstractions.Requirements;
using Vostok.Snitch.Configuration;

namespace Vostok.Snitch.Applications
{
    [RequiresSecretConfiguration(typeof(SnitchSecretSettings))]
    public abstract class SnitchApplicationBase : IVostokApplication
    {
        public abstract Task InitializeAsync(IVostokHostingEnvironment environment);

        public abstract Task RunAsync(IVostokHostingEnvironment environment);
    }
}