using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class NotificationChannels
    {
        public IList<SmtpNotificationChannelDefinition> Smtp { get; set; } = new List<SmtpNotificationChannelDefinition>();

        public IList<WebhookNotificationChannelDefinition> Webhooks { get; set; } = new List<WebhookNotificationChannelDefinition>();

        public IList<PowershellNotificationChannelDefinition> Powershell { get; set; } = new List<PowershellNotificationChannelDefinition>();

        public void Merge(NotificationChannels newChannels)
        {
            foreach(var channel in newChannels.Smtp)
            {
                this.Smtp.Add(channel);
            }

            foreach (var channel in newChannels.Webhooks)
            {
                this.Webhooks.Add(channel);
            }

            foreach (var channel in newChannels.Powershell)
            {
                this.Powershell.Add(channel);
            }
        }
    }
}
