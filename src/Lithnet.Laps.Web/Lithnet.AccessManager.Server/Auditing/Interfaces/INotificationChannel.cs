using System.Collections.Generic;
using System.Collections.Immutable;

namespace Lithnet.AccessManager.Server.Auditing
{
    public interface INotificationChannel
    {
        void ProcessNotification(AuditableAction action, Dictionary<string, string> tokens, IImmutableSet<string> notificationChannels);

        string Name { get; }
    }
}