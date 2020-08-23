using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public class ModelChangedEvent
    {
        public ModelChangedEvent(object sender, string propertyName, bool requiresServiceRestart)
        {
            this.Sender = sender;
            this.PropertyName = propertyName;
            this.RequiresServiceRestart = requiresServiceRestart;
        }

        public object Sender { get; set; }

        public string PropertyName { get; set; }

        public bool RequiresServiceRestart { get; set; }
    }
}
