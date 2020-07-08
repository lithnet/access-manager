using System.DirectoryServices.ActiveDirectory;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IActiveDirectoryForestConfigurationViewModelFactory
    {
        ActiveDirectoryForestConfigurationViewModel CreateViewModel(Forest model);
    }
}