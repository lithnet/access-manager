using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class AzureAdOptions
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string TenantId { get; set; }

        public IList<string> AadIssuerDNs { get; set; } = new List<string> {"DC=net,DC=windows,CN=MS-Organization-Access,OU=82dbaca4-3e81-46ca-9c73-0950c1eaca97"};

        public bool AllowAzureAdJoinedDevices { get; set; }

        public bool AllowAzureAdRegisteredDevices { get; set; }

    }
}
