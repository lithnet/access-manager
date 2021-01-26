using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsFirewallHelper;

namespace Lithnet.AccessManager.Server
{
    public class FirewallProvider : IFirewallProvider
    {
        public const string FirewallRuleName = "Lithnet Access Manager Web Service (HTTP/HTTPS-In)"; // This value also needs to be updated in the installer

        public void ReplaceFirewallRules(int httpPort, int httpsPort, List<Action> rollbackActions)
        {
            this.DeleteFirewallRules(rollbackActions);

            IRule firewallRule = CreateNetFwRule((ushort)httpPort, (ushort)httpsPort);

            FirewallManager.Instance.Rules.Add(firewallRule);

            rollbackActions.Add(() => FirewallManager.Instance.Rules.Remove(firewallRule));
        }

        private IRule CreateNetFwRule(params ushort[] ports)
        {
            IRule firewallRule = FirewallManager.Instance.CreateApplicationRule(FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
                FirewallRuleName,
                FirewallAction.Allow,
               "System",
                FirewallProtocol.TCP
            );

            firewallRule.IsEnable = true;
            firewallRule.Direction = FirewallDirection.Inbound;
            firewallRule.LocalPorts = ports;
            return firewallRule;
        }

        private void DeleteFirewallRules(List<Action> rollbackActions)
        {
            try
            {
                IRule existingFirewallRule = FirewallManager.Instance.Rules.SingleOrDefault(t => string.Equals(t.Name, FirewallRuleName, StringComparison.OrdinalIgnoreCase));
                if (existingFirewallRule != null)
                {
                    FirewallManager.Instance.Rules.Remove(existingFirewallRule);
                    rollbackActions.Add(() => FirewallManager.Instance.Rules.Add(existingFirewallRule));
                }
            }
            catch
            {
                // ignore
            }
        }

    }
}
