using System.Collections.Generic;

namespace Lithnet.AccessManager.Configuration
{
    public class SmtpNotificationChannelDefinition : NotificationChannelDefinition
    {
        public string TemplateSuccess { get; set; }

        public string TemplateFailure { get; set; }

        public IList<string> EmailAddresses { get; set; }
    }
}
