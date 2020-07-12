using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotificationChannelSelectionViewModelFactory : INotificationChannelSelectionViewModelFactory
    {
        private readonly INotificationSubscriptionProvider subscriptionProvider;
        private readonly IEventAggregator eventAggregator;

        public NotificationChannelSelectionViewModelFactory(INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator)
        {
            this.subscriptionProvider = subscriptionProvider;
            this.eventAggregator = eventAggregator;
        }

        public NotificationChannelSelectionViewModel CreateViewModel(AuditNotificationChannels notificationChannels)
        {
            return new NotificationChannelSelectionViewModel(notificationChannels, this.subscriptionProvider, this.eventAggregator);
        }
    }
}
