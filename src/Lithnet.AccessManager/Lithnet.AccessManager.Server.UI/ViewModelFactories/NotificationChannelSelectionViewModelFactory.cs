using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotificationChannelSelectionViewModelFactory : INotificationChannelSelectionViewModelFactory
    {
        private readonly INotificationSubscriptionProvider subscriptionProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly INotifyModelChangedEventPublisher eventPublisher;

        public NotificationChannelSelectionViewModelFactory(INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator, INotifyModelChangedEventPublisher eventPublisher)
        {
            this.subscriptionProvider = subscriptionProvider;
            this.eventAggregator = eventAggregator;
            this.eventPublisher = eventPublisher;
        }

        public NotificationChannelSelectionViewModel CreateViewModel(AuditNotificationChannels notificationChannels)
        {
            return new NotificationChannelSelectionViewModel(notificationChannels, this.subscriptionProvider, this.eventAggregator, this.eventPublisher);
        }
    }
}
