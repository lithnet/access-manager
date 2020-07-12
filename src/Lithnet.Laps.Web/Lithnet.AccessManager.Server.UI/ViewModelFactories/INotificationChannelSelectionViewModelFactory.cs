using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface INotificationChannelSelectionViewModelFactory
    {
        NotificationChannelSelectionViewModel CreateViewModel(AuditNotificationChannels notificationChannels);
    }
}