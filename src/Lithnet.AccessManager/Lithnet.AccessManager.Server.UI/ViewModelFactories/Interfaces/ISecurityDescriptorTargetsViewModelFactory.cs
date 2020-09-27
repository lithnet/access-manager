using System.Collections.Generic;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface ISecurityDescriptorTargetsViewModelFactory
    {
        Task<SecurityDescriptorTargetsViewModel> CreateViewModelAsync(IList<SecurityDescriptorTarget> model);
    }
}