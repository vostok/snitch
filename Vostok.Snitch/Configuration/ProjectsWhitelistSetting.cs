using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Configuration
{
    public class ProjectsWhitelistSetting : HashSet<string>
    {
        public ProjectsWhitelistSetting([NotNull] IEnumerable<string> collection)
            : base(collection)
        {
        }
    }
}