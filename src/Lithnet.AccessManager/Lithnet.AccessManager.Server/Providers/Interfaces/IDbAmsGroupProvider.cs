using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IDbAmsGroupProvider
    {
        IAsyncEnumerable<SecurityIdentifier> GetGroupSidsForDevice(IDevice device);

        Task DeleteGroup(IAmsGroup group);
        
        Task RemoveFromGroup(IAmsGroup group, IDevice device);
        
        IAsyncEnumerable<IDevice> GetMemberDevices(IAmsGroup group);
        
        Task AddToGroup(IAmsGroup group, IDevice device);
        
        Task<IAmsGroup> CloneGroup(IAmsGroup group);
        
        Task<IAmsGroup> CreateGroup();
        
        Task<IAmsGroup> UpdateGroup(IAmsGroup group);
        
        Task<IAmsGroup> GetGroupBySid(string groupSid);
        
        IAsyncEnumerable<IAmsGroup> GetGroups();
    }
}