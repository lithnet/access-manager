using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IActiveDirectoryComputerTargetProvider
    {
        IList<SecurityDescriptorTarget> GetMatchingTargetsForComputer(IActiveDirectoryComputer computer, IEnumerable<SecurityDescriptorTarget> targets);
    }
}