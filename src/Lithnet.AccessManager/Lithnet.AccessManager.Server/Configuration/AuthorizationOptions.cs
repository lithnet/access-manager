using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class AuthorizationOptions
    {
        public List<AuthorizationServerMapping> AuthorizationServerMapping { get; set; } = new List<AuthorizationServerMapping>();

        public List<SecurityDescriptorTarget> ComputerTargets { get; set; } = new List<SecurityDescriptorTarget>();

        public int AuthZCacheDuration { get; set; }
    }
}