using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface ISecurityDescriptorTargetViewModelFactory
    {
        SecurityDescriptorTargetViewModel CreateViewModel(SecurityDescriptorTarget model);
    }
}