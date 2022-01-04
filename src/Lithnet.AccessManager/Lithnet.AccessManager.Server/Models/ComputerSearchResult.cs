using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public class ComputerSearchResult
    {
        public string AuthorityName { get; set; }

        public string Name { get; set; }

        public string DnsName { get; set; }

        public string Key { get; set; }

        public string LastUpdate { get; set; }
    }
}
