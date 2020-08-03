using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotificationSubscriptionProvider : INotificationSubscriptionProvider, IHandle<NotificationSubscriptionChangedEvent>
    {
        private readonly AuditOptions audit;

        private readonly IEventAggregator eventAggregator;

        private readonly object lockObject = new object();

        public BindableCollection<SubscriptionViewModel> Subscriptions { get; }

        public NotificationSubscriptionProvider(AuditOptions audit, IEventAggregator eventAggregator)
        {
            this.audit = audit;
            this.eventAggregator = eventAggregator;
            this.eventAggregator.Subscribe(this);
            this.Subscriptions = new BindableCollection<SubscriptionViewModel>();
            this.Rebuild();
        }

        public BindableCollection<SubscriptionViewModel> GetSubscriptions(IEnumerable<string> ids)
        {
            BindableCollection<SubscriptionViewModel> list = new BindableCollection<SubscriptionViewModel>();

            foreach(string id in ids)
            {
                var item = this.Subscriptions.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));

                if (item != null)
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public bool IsUnique(string name, string id)
        {
            return !this.Subscriptions.Any(t => !string.Equals(t.Id, id, StringComparison.CurrentCultureIgnoreCase) && string.Equals(t.DisplayName, name, StringComparison.CurrentCultureIgnoreCase));
        }

        public void Rebuild()
        {
            lock (this.lockObject)
            {
                this.Subscriptions.Clear();

                foreach (var item in this.audit.NotificationChannels.Powershell)
                {
                    this.Subscriptions.Add(new SubscriptionViewModel(item.Id, item.DisplayName, "PowerShell"));
                }

                foreach (var item in this.audit.NotificationChannels.Smtp)
                {
                    this.Subscriptions.Add(new SubscriptionViewModel(item.Id, item.DisplayName, "Smtp"));
                }

                foreach (var item in this.audit.NotificationChannels.Webhooks)
                {
                    this.Subscriptions.Add(new SubscriptionViewModel(item.Id, item.DisplayName, "Webhook"));
                }
            }
        }

        public void Handle(NotificationSubscriptionChangedEvent message)
        {
            this.Rebuild();
        }
    }
}
