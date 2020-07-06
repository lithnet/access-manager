using System;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Configuration
{
    public class SecurityDescriptorTargetLapsDetails
    {
        public TimeSpan ExpireAfter { get; set; }

        public PasswordStorageLocation RetrievalLocation { get; set; }
    }
}
