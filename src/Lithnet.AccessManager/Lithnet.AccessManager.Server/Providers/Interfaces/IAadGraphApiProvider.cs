using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Graph;

namespace Lithnet.AccessManager.Server
{
    public interface IAadGraphApiProvider
    {
        Task<Device> GetAadDeviceByDeviceIdAsync(string tenant, string deviceId);

        Task<IList<Group>> GetDeviceGroups(string tenant, string objectId);

        Task<Device> GetAadDeviceByIdAsync(string tenant, string objectId);
        
        Task<IList<Group>> GetDeviceGroups(string tenant, string objectId, string selection);

        Task<List<SecurityIdentifier>> GetDeviceGroupSids(string tenant, string objectId);
        
        Task ValidateCredentials(string tenantId, string clientId, ProtectedSecret secret);
        
        Task<string> GetTenantOrgName(string tenantId);
        
        void AddOrUpdateClientCredentials(string tenantId, string clientId, ProtectedSecret secret);
        
        IAsyncEnumerable<Group> FindGroups(string tenant, string searchText);
        
        IAsyncEnumerable<Device> FindDevices(string tenant, string searchText);
        
        Task<Group> GetAadGroupByIdAsync(string tenant, string objectId);
    }
}