namespace Lithnet.Laps.Web.Authorization
{
    public interface IAce
    {
        IAuditNotificationChannels NotificationChannels { get; }

        string Name { get; set; }

        string Sid { get; set; }

        AceType Type { get; set; }
    }
}