using System.Linq;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotificationChannelSelectionViewModel : Screen, IHandle<NotificationSubscriptionChangedEvent>, IHandle<NotificationSubscriptionReloadEvent>
    {
        private readonly INotificationSubscriptionProvider subscriptionProvider;
        public AuditNotificationChannels Model { get; }

        public NotificationChannelSelectionViewModel(AuditNotificationChannels model, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator, INotifyModelChangedEventPublisher eventPublisher)
        {
            this.Model = model;
            this.subscriptionProvider = subscriptionProvider;

            this.SuccessSubscriptions = subscriptionProvider.GetSubscriptions(this.Model.OnSuccess);
            this.FailureSubscriptions = subscriptionProvider.GetSubscriptions(this.Model.OnFailure);

            this.AvailableSuccessSubscriptions = new BindableCollection<SubscriptionViewModel>(subscriptionProvider.Subscriptions.Except(this.SuccessSubscriptions));
            this.AvailableFailureSubscriptions = new BindableCollection<SubscriptionViewModel>(subscriptionProvider.Subscriptions.Except(this.FailureSubscriptions));

            eventAggregator.Subscribe(this);
            eventPublisher.Register(this);
        }

        [NotifyModelChangedCollection]
        public BindableCollection<SubscriptionViewModel> SuccessSubscriptions { get; set; }

        [NotifyModelChangedCollection]
        public BindableCollection<SubscriptionViewModel> FailureSubscriptions { get; set; }

        public BindableCollection<SubscriptionViewModel> AvailableSuccessSubscriptions { get; }

        public BindableCollection<SubscriptionViewModel> AvailableFailureSubscriptions { get; }

        public SubscriptionViewModel SelectedSuccessSubscription { get; set; }

        public SubscriptionViewModel SelectedFailureSubscription { get; set; }

        public SubscriptionViewModel SelectedAvailableSuccessSubscription { get; set; }

        public SubscriptionViewModel SelectedAvailableFailureSubscription { get; set; }

        public void AddSuccess()
        {
            this.Model.OnSuccess.Add(this.SelectedAvailableSuccessSubscription.Id);
            this.SuccessSubscriptions.Add(this.SelectedAvailableSuccessSubscription);
            this.AvailableSuccessSubscriptions.Remove(this.SelectedAvailableSuccessSubscription);
        }

        public bool CanAddSuccess => this.SelectedAvailableSuccessSubscription != null;

        public void RemoveSuccess()
        {
            string matchingItem = this.Model.OnSuccess.FirstOrDefault(t => string.Equals(t, this.SelectedSuccessSubscription.Id, System.StringComparison.OrdinalIgnoreCase));
            this.Model.OnSuccess.Remove(matchingItem);
            this.AvailableSuccessSubscriptions.Add(this.SelectedSuccessSubscription);
            this.SuccessSubscriptions.Remove(this.SelectedSuccessSubscription);
        }

        public bool CanRemoveSuccess => this.SelectedSuccessSubscription != null;

        public void AddFailure()
        {
            this.Model.OnFailure.Add(this.SelectedAvailableFailureSubscription.Id);
            this.FailureSubscriptions.Add(this.SelectedAvailableFailureSubscription);
            this.AvailableFailureSubscriptions.Remove(this.SelectedAvailableFailureSubscription);
        }

        public bool CanAddFailure => this.SelectedAvailableFailureSubscription != null;

        public void RemoveFailure()
        {
            string matchingItem = this.Model.OnFailure.FirstOrDefault(t => string.Equals(t, this.SelectedFailureSubscription.Id, System.StringComparison.OrdinalIgnoreCase));
            this.Model.OnFailure.Remove(matchingItem);
            this.AvailableFailureSubscriptions.Add(this.SelectedFailureSubscription);
            this.FailureSubscriptions.Remove(this.SelectedFailureSubscription);
        }

        public bool CanRemoveFailure => this.SelectedFailureSubscription != null;

        public void Handle(NotificationSubscriptionReloadEvent message)
        {
            this.AvailableSuccessSubscriptions.Clear();
            this.AvailableSuccessSubscriptions.AddRange(subscriptionProvider.Subscriptions.Except(this.SuccessSubscriptions));

            this.AvailableFailureSubscriptions.Clear();
            this.AvailableFailureSubscriptions.AddRange(subscriptionProvider.Subscriptions.Except(this.FailureSubscriptions));
        }

        public void Handle(NotificationSubscriptionChangedEvent message)
        {
            if (message.IsTransient)
            {
                return;
            }

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