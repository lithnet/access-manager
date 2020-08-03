using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public class ModelChangedEvent
    {
        public ModelChangedEvent(object sender, string propertyName)
        {
            this.Sender = sender;
            this.PropertyName = propertyName;
        }

        public object Sender { get; set; }

        public string PropertyName { get; set; }
    }
}
