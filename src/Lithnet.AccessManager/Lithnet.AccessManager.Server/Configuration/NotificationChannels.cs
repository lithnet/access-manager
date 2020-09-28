using System.Collections.Generic;
using System.Linq;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class NotificationChannels
    {
        public IList<SmtpNotificationChannelDefinition> Smtp { get; set; } = new List<SmtpNotificationChannelDefinition>();

        public IList<WebhookNotificationChannelDefinition> Webhooks { get; set; } = new List<WebhookNotificationChannelDefinition>();

        public IList<PowershellNotificationChannelDefinition> Powershell { get; set; } = new List<PowershellNotificationChannelDefinition>();

        public void Merge(NotificationChannels newChannels)
        {
            foreach (var channel in newChannels.Smtp)
            {
                this.EnsureUniqueChannelName(channel);
                this.Smtp.Add(channel);
            }

            foreach (var channel in newChannels.Webhooks)
            {
                this.EnsureUniqueChannelName(channel);
                this.Webhooks.Add(channel);
            }

            foreach (var channel in newChannels.Powershell)
            {
                this.EnsureUniqueChannelName(channel);
                this.Powershell.Add(channel);
            }
        }

        private void EnsureUniqueChannelName(NotificationChannelDefinition channel, string proposedChannelName = null, int count = 0)
        {
            proposedChannelName ??= channel.DisplayName;

            if (this.Smtp.Any(t => string.Equals(proposedChannelName, t.DisplayName)) ||
                this.Webhooks.Any(t => string.Equals(proposedChannelName, t.DisplayName)) ||
                this.Powershell.Any(t => string.Equals(proposedChannelName, t.DisplayName)))
            {
                count++;
                proposedChannelName = $"{channel.DisplayName} - {count}";

                this.EnsureUniqueChannelName(channel, proposedChannelName, count);
                return;
            }

            channel.DisplayName = proposedChannelName;
        } 
    }
}
