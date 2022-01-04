using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public class AvailableRole
    {
        public string Name { get; set; }

        public string Key { get; set; }

        public bool ReasonRequired { get; set; }

        public TimeSpan MaximumRequestDuration { get; set; }
    }
}
