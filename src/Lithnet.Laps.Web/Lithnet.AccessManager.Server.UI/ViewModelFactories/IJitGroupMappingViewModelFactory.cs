using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IJitGroupMappingViewModelFactory
    {
        JitGroupMappingViewModel CreateViewModel(JitGroupMapping model);
    }
}