using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IAmsGroupProvider
    {
        Task DeleteGroup(IAmsGroup group);
        
        Task<IAmsGroup> CloneGroup(IAmsGroup group);

        Task<IAmsGroup> CreateGroup();

        Task<IAmsGroup> UpdateGroup(IAmsGroup group);

        IAsyncEnumerable<IAmsGroup> GetGroups();

        IAsyncEnumerable<SecurityIdentifier> GetGroupSidsForDevice(IDevice device);
        Task RemoveFromGroup(IAmsGroup group, IDevice device);
        Task AddToGroup(IAmsGroup group, IDevice device);
        IAsyncEnumerable<IDevice> GetMemberDevices(IAmsGroup group);
    }
}