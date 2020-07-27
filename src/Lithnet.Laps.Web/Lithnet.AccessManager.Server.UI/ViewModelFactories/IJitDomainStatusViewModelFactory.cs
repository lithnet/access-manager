using System.DirectoryServices.ActiveDirectory;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IJitDomainStatusViewModelFactory
    {
        JitDomainStatusViewModel CreateViewModel(Domain model, JitDynamicGroupMapping mapping);
    }
}