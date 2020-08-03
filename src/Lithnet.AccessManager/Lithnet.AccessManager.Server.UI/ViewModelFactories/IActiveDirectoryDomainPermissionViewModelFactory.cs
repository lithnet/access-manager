using System.DirectoryServices.ActiveDirectory;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IActiveDirectoryDomainPermissionViewModelFactory
    {
        ActiveDirectoryDomainPermissionViewModel CreateViewModel(Domain model);
    }
}