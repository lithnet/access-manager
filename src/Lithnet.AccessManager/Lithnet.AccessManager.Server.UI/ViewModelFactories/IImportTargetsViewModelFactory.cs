using Lithnet.AccessManager.Server.UI.ViewModels;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IImportTargetsViewModelFactory
    {
        ImportSettingsViewModel CreateViewModel();
    }
}