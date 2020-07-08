using Lithnet.AccessManager.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface INotificationChannelDefinitionViewModelFactory<TModel, TViewModel> where TModel : NotificationChannelDefinition where TViewModel : NotificationChannelDefinitionViewModel<TModel>
    {
        TModel CreateModel();
        TViewModel CreateViewModel(TModel model);
    }
}