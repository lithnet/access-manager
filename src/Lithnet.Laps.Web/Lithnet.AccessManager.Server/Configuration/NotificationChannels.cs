using System.Collections.Generic;

namespace Lithnet.AccessManager.Configuration
{
    public class NotificationChannels
    {
        public IList<SmtpNotificationChannelDefinition> Smtp { get; set; } = new List<SmtpNotificationChannelDefinition>();

        public IList<WebhookNotificationChannelDefinition> Webhooks { get; set; } = new List<WebhookNotificationChannelDefinition>();

        public IList<PowershellNotificationChannelDefinition> Powershell { get; set; } = new List<PowershellNotificationChannelDefinition>();
    }
}
