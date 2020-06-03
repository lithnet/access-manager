using System.Collections.Generic;

namespace Lithnet.Laps.Web.Internal
{
    public interface INotificationChannel
    {
        void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens);

        string Name { get; }
    }
}