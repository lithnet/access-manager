using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface ISecurityDescriptorTargetViewModelFactory
    {
        Task<SecurityDescriptorTargetViewModel> CreateViewModelAsync(SecurityDescriptorTarget model, SecurityDescriptorTargetViewModelDisplaySettings settings);
    }
}