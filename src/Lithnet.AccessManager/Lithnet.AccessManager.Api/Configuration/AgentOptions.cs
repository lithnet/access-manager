using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class AgentOptions
    {
        public bool AllowAadAuth { get; set; } = false;

        public bool AllowSelfSignedAuth { get; set; } = true;

        public bool AutoApproveSelfSignedAuth { get; set; } = false;

        public bool AllowWindowsAuth { get; set; } = true;

        public bool AllowAzureAdJoinedDevices { get; set; } = true;

        public bool AllowAzureAdRegisteredDevices { get; set; } = false;

        public int EncryptionCertificateCacheDurationMinutes { get; set; } = 15;
    }
}
