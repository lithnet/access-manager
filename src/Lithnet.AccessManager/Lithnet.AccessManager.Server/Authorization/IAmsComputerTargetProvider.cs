using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAmsComputerTargetProvider
    {
        List<SecurityDescriptorTarget> GetMatchingTargetsForComputer(Device computer, IEnumerable<SecurityDescriptorTarget> targets);
    }
}