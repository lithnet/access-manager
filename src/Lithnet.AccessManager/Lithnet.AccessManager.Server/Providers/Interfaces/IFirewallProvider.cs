using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager.Server
{
    public interface IFirewallProvider
    {
        void ReplaceFirewallRules(int httpPort, int httpsPort, List<Action> rollbackActions);
    }
}