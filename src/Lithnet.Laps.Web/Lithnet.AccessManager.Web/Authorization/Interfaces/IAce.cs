namespace Lithnet.AccessManager.Web.Authorization
{
    public interface IAce
    {
        IAuditNotificationChannels NotificationChannels { get; }

        string Trustee { get; }

        string Sid { get; }

        AceType Type { get; }

        AccessMask Access { get; }
    }
}