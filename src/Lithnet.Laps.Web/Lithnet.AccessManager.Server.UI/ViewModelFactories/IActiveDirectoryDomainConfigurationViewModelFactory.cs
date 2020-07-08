using System.DirectoryServices.ActiveDirectory;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IActiveDirectoryDomainConfigurationViewModelFactory
    {
        ActiveDirectoryDomainConfigurationViewModel CreateViewModel(Domain model);
    }
}