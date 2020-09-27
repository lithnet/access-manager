using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class AuditNotificationChannels
    {
        public HashSet<string> OnFailure { get; set; } = new HashSet<string>();

        public HashSet<string> OnSuccess { get; set; } = new HashSet<string>();
    }
}
