using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Xml;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuditingViewModel : Conductor<PropertyChangedBase>.Collection.OneActive
    {
        private readonly INotificationSubscriptionProvider subscriptions;

        private readonly IEventAggregator eventAggregator;

        private readonly AuditOptions model;

        public AuditingViewModel(AuditOptions model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator)
        {
            this.DisplayName = "Auditing";

            this.model = model;
            this.subscriptions = subscriptionProvider;
            this.Powershell = new PowershellNotificationChannelDefinitionsViewModel(model.NotificationChannels.Powershell, dialogCoordinator, subscriptionProvider, eventAggregator);
            this.Webhook = new WebhookNotificationChannelDefinitionsViewModel(this.model.NotificationChannels.Webhooks, dialogCoordinator, subscriptionProvider, eventAggregator);
            this.Smtp = new SmtpNotificationChannelDefinitionsViewModel(this.model.NotificationChannels.Smtp, dialogCoordinator, subscriptionProvider, eventAggregator);

            this.Items.Add(this.Powershell);
            this.Items.Add(this.Webhook);
            this.Items.Add(this.Smtp);
            this.ActivateItem(this.Powershell);

            this.Notifications = new NotificationChannelSelectionViewModel(this.model.GlobalNotifications, subscriptionProvider, eventAggregator);
        }

        public NotificationChannelSelectionViewModel Notifications { get; }

        private PowershellNotificationChannelDefinitionsViewModel Powershell { get; }

        private WebhookNotificationChannelDefinitionsViewModel Webhook { get; }

        private SmtpNotificationChannelDefinitionsViewModel Smtp { get; }


    }
}
