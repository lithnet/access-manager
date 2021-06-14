using System.Collections.Generic;

namespace Lithnet.AccessManager.Api.Shared
{
    public class AgentCheckIn
    {
        public string OperatingSystem { get; set; }

        public string OperationSystemVersion { get; set; }

        public string AgentVersion { get; set; }

        public string Hostname { get; set; }

        public string DnsName { get; set; }
    }
}