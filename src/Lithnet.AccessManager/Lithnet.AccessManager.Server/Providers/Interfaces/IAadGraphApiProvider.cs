using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Lithnet.AccessManager.Server
{
    public interface IAadGraphApiProvider
    {
        Task<Microsoft.Graph.Device> GetAadDeviceByDeviceIdAsync(string deviceId);

        Task<IList<Group>> GetDeviceGroups(string objectId);

        Task<Microsoft.Graph.Device> GetAadDeviceByIdAsync(string objectId);
        Task<IList<Group>> GetDeviceGroups(string objectId, string selection);
        Task<List<SecurityIdentifier>> GetDeviceGroupSids(string objectId);
    }
}