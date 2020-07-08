using Lithnet.AccessManager.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface ISecurityDescriptorTargetViewModelFactory
    {
        SecurityDescriptorTargetViewModel CreateViewModel(SecurityDescriptorTarget model);
    }
}