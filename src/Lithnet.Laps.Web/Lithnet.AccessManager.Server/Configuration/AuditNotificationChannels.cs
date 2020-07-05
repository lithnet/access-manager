using System.Collections.Generic;

namespace Lithnet.AccessManager.Configuration
{
    public class AuditNotificationChannels
    {
        public IList<string> OnFailure { get; set; } = new List<string>();

        public IList<string> OnSuccess { get; set; } = new List<string>();
    }
}
