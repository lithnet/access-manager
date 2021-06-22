using System.Collections.Generic;

namespace Lithnet.AccessManager.Api.Shared
{
    public class AgentAuthentication
    {
        public List<string> AllowedOptions { get; set; } = new List<string>();

        public List<string> AllowedAzureAdTenants { get; set; } = new List<string>();
    }
}