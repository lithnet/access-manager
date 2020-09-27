using System.DirectoryServices.ActiveDirectory;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IActiveDirectoryForestSchemaViewModelFactory
    {
        ActiveDirectoryForestSchemaViewModel CreateViewModel(Forest model);
    }
}