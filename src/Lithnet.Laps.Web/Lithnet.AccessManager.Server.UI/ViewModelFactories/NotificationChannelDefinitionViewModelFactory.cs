using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;

namespace Lithnet.AccessManager.Server.UI
{
    public abstract class NotificationChannelDefinitionViewModelFactory<TModel, TViewModel> : INotificationChannelDefinitionViewModelFactory<TModel, TViewModel> where TModel : NotificationChannelDefinition where TViewModel : NotificationChannelDefinitionViewModel<TModel>
    {
        protected NotificationChannelDefinitionViewModelFactory()
        {
        }

        public abstract TModel CreateModel();

        public abstract TViewModel CreateViewModel(TModel model);
    }
}
