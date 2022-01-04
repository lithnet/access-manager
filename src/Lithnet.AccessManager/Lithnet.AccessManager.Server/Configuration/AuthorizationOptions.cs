using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class AuthorizationOptions
    {
        public List<AuthorizationServerMapping> AuthorizationServerMapping { get; set; } = new List<AuthorizationServerMapping>();

        public List<SecurityDescriptorTarget> ComputerTargets { get; set; } = new List<SecurityDescriptorTarget>();

        public List<RoleSecurityDescriptorTarget> Roles { get; set; } = new List<RoleSecurityDescriptorTarget>();
        
        public int AuthZCacheDuration { get; set; }
    }
}