using System.Collections.Generic;
using Lithnet.AccessManager.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface INotificationChannelDefinitionsViewModelFactory<TModel, TViewModel> where TModel : NotificationChannelDefinition where TViewModel : NotificationChannelDefinitionViewModel<TModel>
    {
        NotificationChannelDefinitionsViewModel<TModel, TViewModel> CreateViewModel(IList<TModel> model);
    }
}