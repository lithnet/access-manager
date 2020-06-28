using System.Collections.Generic;

namespace Lithnet.AccessManager.Configuration
{
    public abstract class NotificationChannelDefinition
    {
        public bool Enabled { get; set; }

        public string ID { get; set; }

        public bool Mandatory { get; set; }
    }
}
