using System.Collections.Generic;
using JetBrains.Annotations;

namespace Vostok.Snitch.Configuration
{
    public class EnvironmentsWhitelistSetting : HashSet<string>
    {
        public EnvironmentsWhitelistSetting([NotNull] IEnumerable<string> collection)
            : base(collection)
        {
        }
    }
}