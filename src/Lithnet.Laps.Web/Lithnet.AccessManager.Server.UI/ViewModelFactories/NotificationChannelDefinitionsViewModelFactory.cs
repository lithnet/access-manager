using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public abstract class NotificationChannelDefinitionsViewModelFactory<TModel, TViewModel> : INotificationChannelDefinitionsViewModelFactory<TModel, TViewModel> where TModel : NotificationChannelDefinition where TViewModel : NotificationChannelDefinitionViewModel<TModel>
    {
        private readonly NotificationChannelDefinitionViewModelFactory<TModel, TViewModel> factory;

        protected NotificationChannelDefinitionsViewModelFactory(NotificationChannelDefinitionViewModelFactory<TModel, TViewModel> factory)
        {
            this.factory = factory;
        }

        public abstract NotificationChannelDefinitionsViewModel<TModel, TViewModel> CreateViewModel(IList<TModel> model);
    }
}
