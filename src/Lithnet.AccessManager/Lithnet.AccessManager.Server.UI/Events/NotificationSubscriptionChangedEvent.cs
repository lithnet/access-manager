using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotificationSubscriptionChangedEvent
    {
        public ModificationType ModificationType { get; set; }

        public NotificationChannelDefinition ModifiedObject { get; set; }

        public bool IsTransient { get; set; }
    }
}
