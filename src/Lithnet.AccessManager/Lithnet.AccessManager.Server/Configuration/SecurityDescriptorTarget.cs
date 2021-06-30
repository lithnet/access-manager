using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server.Configuration
{
    [DebuggerDisplay("{Id} - {Target}")]
    public class SecurityDescriptorTarget
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public bool Disabled { get; set; }

        public DateTime? Expiry { get; set; }

        public string Target { get; set; }

        public string CachedTargetName { get; set; }

        public string TargetObjectId { get; set; }

        public string TargetAuthorityId { get; set; }

        public string Description { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TargetType Type { get; set; } = TargetType.AdContainer;

        [JsonConverter(typeof(StringEnumConverter))]
        public AuthorizationMode AuthorizationMode { get; set; } = AuthorizationMode.SecurityDescriptor;

        public string SecurityDescriptor { get; set; } = "O:SYD:";

        public string Script { get; set; }

        public SecurityDescriptorTargetJitDetails Jit { get; set; } = new SecurityDescriptorTargetJitDetails();

        public SecurityDescriptorTargetLapsDetails Laps { get; set; } = new SecurityDescriptorTargetLapsDetails();

        public AuditNotificationChannels Notifications { get; set; } = new AuditNotificationChannels();

        public string CreatedBy { get; set; }

        public DateTime? Created { get; set; }

        public string LastModifiedBy { get; set; }

        public DateTime? LastModified { get; set; }

        public string Notes { get; set; }

        public bool IsActive()
        {
            return !this.IsInactive();
        }

        public bool IsInactive()
        {
            return this.Disabled || this.HasExpired();
        }

        public bool HasExpired()
        {
            return this.Expiry != null && DateTime.UtcNow > this.Expiry.Value.ToUniversalTime();
        }

        public override string ToString()
        {
            return this.Id;
        }
    }
}