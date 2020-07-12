using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.UI
{
    public interface ISecurityDescriptorTargetsViewModelFactory
    {
        SecurityDescriptorTargetsViewModel CreateViewModel(IList<SecurityDescriptorTarget> model);
    }
}