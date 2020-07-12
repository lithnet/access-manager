using System;
using System.Security.Principal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class SecurityDescriptorTarget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Target { get; set; }

        public string Description { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TargetType Type { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AuthorizationMode AuthorizationMode { get; set; } = AuthorizationMode.SecurityDescriptor;

        public string SecurityDescriptor { get; set; } = "O:SYD:";

        public string Script { get; set; }

        public SecurityDescriptorTargetJitDetails Jit { get; set; } = new SecurityDescriptorTargetJitDetails();

        public SecurityDescriptorTargetLapsDetails Laps { get; set; } = new SecurityDescriptorTargetLapsDetails();

        public AuditNotificationChannels Notifications { get; set; } = new AuditNotificationChannels();

        public SecurityIdentifier GetTargetAsSid()
        {
            if (this.Target == null)
            {
                throw new ArgumentNullException(nameof(this.Target), "The target ID was null");
            }

            SecurityIdentifier s = new SecurityIdentifier(this.Target);
            return s;
        }
    }
}