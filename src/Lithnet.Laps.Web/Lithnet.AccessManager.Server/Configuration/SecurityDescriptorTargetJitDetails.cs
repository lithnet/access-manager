using System;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class SecurityDescriptorTargetJitDetails
    {
        public string AuthorizingGroup { get; set; }

        public TimeSpan ExpireAfter { get; set; }
    }
}