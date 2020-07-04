namespace Lithnet.AccessManager.Configuration
{
    public class AuditOptions 
    {
        public NotificationChannels NotificationChannels { get; set; } = new NotificationChannels();

        public AuditNotificationChannels GlobalNotifications { get; set; } = new AuditNotificationChannels();
    }
}