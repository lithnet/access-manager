using Lithnet.AccessManager.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface INotificationChannelSelectionViewModelFactory
    {
        NotificationChannelSelectionViewModel CreateViewModel(AuditNotificationChannels notificationChannels);
    }
}