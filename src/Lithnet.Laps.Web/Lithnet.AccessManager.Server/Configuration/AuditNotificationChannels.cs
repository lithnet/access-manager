using System.Collections.Generic;

namespace Lithnet.AccessManager.Configuration
{
    public class AuditNotificationChannels
    {
        public IList<string> OnFailure { get; set; }


        public IList<string> OnSuccess { get; set; }
    }
}
