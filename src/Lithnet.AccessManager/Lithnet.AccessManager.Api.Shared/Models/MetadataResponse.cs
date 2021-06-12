using System;
using System.Text;

namespace Lithnet.AccessManager.Api.Shared
{
    public class MetadataResponse
    {
        public AgentAuthentication AgentAuthentication { get; set; } = new AgentAuthentication();

        public PasswordManagement PasswordManagement { get; set; } = new PasswordManagement();
    }
}
