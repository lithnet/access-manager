using Lithnet.AccessManager.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IJitConfigurationViewModelFactory
    {
        JitConfigurationViewModel CreateViewModel();
    }
}