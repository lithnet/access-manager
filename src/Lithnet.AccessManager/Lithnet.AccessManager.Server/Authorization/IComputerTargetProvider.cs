using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IComputerTargetProvider
    {
        IList<SecurityDescriptorTarget> GetMatchingTargetsForComputer(IComputer computer, IEnumerable<SecurityDescriptorTarget> targets);
    }
}