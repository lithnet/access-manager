using System;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotificationChannelSelectionViewModelFactory : IViewModelFactory<NotificationChannelSelectionViewModel, AuditNotificationChannels>
    {
        private readonly INotificationSubscriptionProvider subscriptionProvider;
        private readonly Func<IEventAggregator> eventAggregator;
        private readonly Func<INotifyModelChangedEventPublisher> eventPublisher;

        public NotificationChannelSelectionViewModelFactory(INotificationSubscriptionProvider subscriptionProvider, Func<IEventAggregator> eventAggregator, Func<INotifyModelChangedEventPublisher> eventPublisher)
        {
            this.subscriptionProvider = subscriptionProvider;
            this.eventAggregator = eventAggregator;
            this.eventPublisher = eventPublisher;
        }

        public NotificationChannelSelectionViewModel CreateViewModel(AuditNotificationChannels notificationChannels)
        {
            return new NotificationChannelSelectionViewModel(notificationChannels, this.subscriptionProvider, this.eventAggregator.Invoke(), this.eventPublisher.Invoke());
        }
    }
}
