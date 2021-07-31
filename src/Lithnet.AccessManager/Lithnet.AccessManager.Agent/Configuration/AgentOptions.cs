using Lithnet.AccessManager.Api.Shared;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class AgentOptions
    {
        public int Interval { get; set; } = 60;

        public bool Enabled { get; set; } = true;

        public int CheckInIntervalHours { get; set; } = 24;

        [JsonIgnore]
        public bool AmsServerManagementEnabled => true;

        public bool EnableAdminAccount { get; set; } = true;

        public bool AmsPasswordStorageEnabled { get; set; } = true;

        public AgentAuthenticationMode AuthenticationMode { get; set; } = AgentAuthenticationMode.None;

        public string Server { get; set; }

        public List<string> AzureTenantIDs { get; set; } = new List<string>();

        public bool RegisterSecondaryCredentialsForAadr { get; set; } = true;

        public bool RegisterSecondaryCredentialsForAadj { get; set; } = false;
    }
}
