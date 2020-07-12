using System;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class SecurityDescriptorTargetJitDetails
    {
        public string AuthorizingGroup { get; set; }

        public TimeSpan ExpireAfter { get; set; }
    }
}