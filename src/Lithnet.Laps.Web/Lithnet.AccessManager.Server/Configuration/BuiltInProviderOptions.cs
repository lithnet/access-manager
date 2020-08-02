using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class BuiltInProviderOptions
    {
        public List<AuthorizationServerMapping> AuthorizationServerMapping { get; set; } = new List<AuthorizationServerMapping>();

        public IList<SecurityDescriptorTarget> Targets { get; set; }

        public int AuthZCacheDuration { get; set; }
    }
}
