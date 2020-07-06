using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Configuration
{
    public class SecurityDescriptorTarget
    {
        public TargetType Type { get; set; }

        public string Id { get; set; }

        public SecurityDescriptorTargetJitDetails Jit { get; set; } = new SecurityDescriptorTargetJitDetails();

        public SecurityDescriptorTargetLapsDetails Laps { get; set; } = new SecurityDescriptorTargetLapsDetails();

        public AuditNotificationChannels Notifications { get; set; } = new AuditNotificationChannels();

        public AuthorizationMode AuthorizationMode { get; set; } = AuthorizationMode.SecurityDescriptor;

        public string SecurityDescriptor { get; set; }

        public string Script { get; set; }
    }
}