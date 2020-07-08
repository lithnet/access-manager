using System.Windows;
using Lithnet.AccessManager.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public abstract class NotificationChannelDefinitionViewModel<TModel> : ValidatingModelBase, IViewAware where TModel : NotificationChannelDefinition
    {
        protected NotificationChannelDefinitionViewModel(TModel model)
        {
            this.Model = model;
        }

        public TModel Model { get; }

        public bool Enabled { get => this.Model.Enabled; set => this.Model.Enabled = value; }

        public string DisplayName { get => this.Model.DisplayName; set => this.Model.DisplayName = value; }

        public string Id { get => this.Model.Id; set => this.Model.Id = value; }

        public bool Mandatory { get => this.Model.Mandatory; set => this.Model.Mandatory = value; }

        public UIElement View { get; private set; }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}
