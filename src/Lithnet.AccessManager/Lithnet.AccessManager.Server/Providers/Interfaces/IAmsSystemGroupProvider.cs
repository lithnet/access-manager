using System.Collections.Generic;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IAmsSystemGroupProvider
    {
        IEnumerable<SecurityIdentifier> GetGroupSidsForDevice(IDevice device);

        IAsyncEnumerable<IDevice> GetMemberDevices(IAmsGroup group);

        IAmsGroup GetGroupBySid(string groupSid);

        IEnumerable<IAmsGroup> GetGroups();
    }
}