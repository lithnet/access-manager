using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Lithnet.AccessManager.Configuration;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    [AddINotifyPropertyChangedInterface]
    public class SubscriptionViewModel : IEquatable<SubscriptionViewModel>
    {
        public SubscriptionViewModel(NotificationChannelDefinition channel)
        {
            this.Id = channel.Id;
            this.DisplayName = channel.DisplayName;
            this.Type = channel is PowershellNotificationChannelDefinition ? "PowerShell" : channel is SmtpNotificationChannelDefinition ? "SMTP" : "Webhook";
        }

        public SubscriptionViewModel(string id, string name, string type)
        {
            this.Id = id;
            this.DisplayName = name;
            this.Type = type;
        }

        public string Type { get; set; }

        public string Id { get; set; }

        public string DisplayName { get; set; }

        public string ControlDisplayName => $"{DisplayName} [{Type}]";

        public override bool Equals(object obj)
        {
            return Equals(obj as SubscriptionViewModel);
        }

        public bool Equals(SubscriptionViewModel other)
        {
            return other != null &&
                   string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public static bool operator ==(SubscriptionViewModel left, SubscriptionViewModel right)
        {
            return EqualityComparer<SubscriptionViewModel>.Default.Equals(left, right);
        }

        public static bool operator !=(SubscriptionViewModel left, SubscriptionViewModel right)
        {
            return !(left == right);
        }
    }
}
