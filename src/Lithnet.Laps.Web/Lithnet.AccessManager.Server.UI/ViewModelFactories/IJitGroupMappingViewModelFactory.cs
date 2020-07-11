using Lithnet.AccessManager.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IJitGroupMappingViewModelFactory
    {
        JitGroupMappingViewModel CreateViewModel(JitGroupMapping model);
    }
}