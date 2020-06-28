using System.Collections.Generic;

namespace Lithnet.AccessManager.Configuration
{
    public class NotificationChannels
    {
        public IList<WebhookNotificationChannelDefinition> Webhooks { get; set; }

        public IList<PowershellNotificationChannelDefinition> Powershell { get; set; }

        public IList<SmtpNotificationChannelDefinition> Smtp { get; set; }
    }
}
