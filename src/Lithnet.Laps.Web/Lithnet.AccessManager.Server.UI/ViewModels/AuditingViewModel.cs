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
    public class AuditingViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHandle<NotificationSubscriptionChangedEvent>
    {
        private readonly INotificationSubscriptionProvider subscriptions;

        private AuditOptions model;

        private IEventAggregator eventAggregator;

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

            this.SuccessSubscriptions = subscriptionProvider.GetSubscriptions(this.model.GlobalNotifications.OnSuccess);
            this.FailureSubscriptions = subscriptionProvider.GetSubscriptions(this.model.GlobalNotifications.OnFailure);

            this.AvailableSuccessSubscriptions = new BindableCollection<SubscriptionViewModel>(subscriptionProvider.Subscriptions.Except(this.SuccessSubscriptions));
            this.AvailableFailureSubscriptions = new BindableCollection<SubscriptionViewModel>(subscriptionProvider.Subscriptions.Except(this.FailureSubscriptions));

            this.eventAggregator = eventAggregator;
            this.eventAggregator.Subscribe(this);
        }

        public BindableCollection<SubscriptionViewModel> SuccessSubscriptions { get; set; }

        public BindableCollection<SubscriptionViewModel> FailureSubscriptions { get; set; }

        public BindableCollection<SubscriptionViewModel> AvailableSuccessSubscriptions { get; }

        public BindableCollection<SubscriptionViewModel> AvailableFailureSubscriptions { get; }

        private PowershellNotificationChannelDefinitionsViewModel Powershell { get; }

        private WebhookNotificationChannelDefinitionsViewModel Webhook { get; }

        private SmtpNotificationChannelDefinitionsViewModel Smtp { get; }

        public SubscriptionViewModel SelectedSuccessSubscription { get; set; }

        public SubscriptionViewModel SelectedFailureSubscription { get; set; }

        public SubscriptionViewModel SelectedAvailableSuccessSubscription { get; set; }

        public SubscriptionViewModel SelectedAvailableFailureSubscription { get; set; }

        public void AddSuccess()
        {
            this.model.GlobalNotifications.OnSuccess.Add(this.SelectedAvailableSuccessSubscription.Id);
            this.SuccessSubscriptions.Add(this.SelectedAvailableSuccessSubscription);
            this.AvailableSuccessSubscriptions.Remove(this.SelectedAvailableSuccessSubscription);
        }

        public bool CanAddSuccess => this.SelectedAvailableSuccessSubscription != null;

        public void RemoveSuccess()
        {
            string matchingItem = this.model.GlobalNotifications.OnSuccess.FirstOrDefault(t => string.Equals(t, this.SelectedSuccessSubscription.Id, System.StringComparison.OrdinalIgnoreCase));
            this.model.GlobalNotifications.OnSuccess.Remove(matchingItem);
            this.AvailableSuccessSubscriptions.Add(this.SelectedSuccessSubscription);
            this.SuccessSubscriptions.Remove(this.SelectedSuccessSubscription);
        }

        public bool CanRemoveSuccess => this.SelectedSuccessSubscription != null;

        public void AddFailure()
        {
            this.model.GlobalNotifications.OnFailure.Add(this.SelectedAvailableFailureSubscription.Id);
            this.FailureSubscriptions.Add(this.SelectedAvailableFailureSubscription);
            this.AvailableFailureSubscriptions.Remove(this.SelectedAvailableFailureSubscription);
        }

        public bool CanAddFailure => this.SelectedAvailableFailureSubscription != null;

        public void RemoveFailure()
        {
            string matchingItem = this.model.GlobalNotifications.OnFailure.FirstOrDefault(t => string.Equals(t, this.SelectedFailureSubscription.Id, System.StringComparison.OrdinalIgnoreCase));
            this.model.GlobalNotifications.OnFailure.Remove(matchingItem);
            this.AvailableFailureSubscriptions.Add(this.SelectedFailureSubscription);
            this.FailureSubscriptions.Remove(this.SelectedFailureSubscription);
        }

        public bool CanRemoveFailure => this.SelectedFailureSubscription != null;


        public void Handle(NotificationSubscriptionChangedEvent message)
        {
            var sub = new SubscriptionViewModel(message.ModifiedObject);

            if (message.ModificationType == ModificationType.Added)
            {
                this.AvailableSuccessSubscriptions.Add(sub);
                this.AvailableFailureSubscriptions.Add(sub);
            }
            else if (message.ModificationType == ModificationType.Deleted)
            {
                this.AvailableSuccessSubscriptions.Remove(sub);
                this.SuccessSubscriptions.Remove(sub);

                this.AvailableFailureSubscriptions.Remove(sub);
                this.FailureSubscriptions.Remove(sub);
            }
            else if (message.ModificationType == ModificationType.Modified)
            {
                foreach (var item in this.SuccessSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }

                foreach (var item in this.AvailableSuccessSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }

                foreach (var item in this.FailureSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }

                foreach (var item in this.AvailableFailureSubscriptions)
                {
                    if (item == sub)
                    {
                        item.DisplayName = sub.DisplayName;
                    }
                }
            }
        }

    }
}
