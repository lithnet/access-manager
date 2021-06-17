using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public interface IDeviceProvider
    {
        Task<Device> GetOrCreateDeviceAsync(Microsoft.Graph.Device device, string authority);

        Task<Device> GetOrCreateDeviceAsync(IActiveDirectoryComputer principal, string authority);

        Task<Device> CreateDeviceAsync(IActiveDirectoryComputer computer, string authority, string deviceId);

        Task<Device> GetDeviceAsync(AuthorityType authorityType, string authority, string authorityDeviceId);

        Task<Device> GetDeviceAsync(X509Certificate2 certificate);

        Task<Device> CreateDeviceAsync(Device device, X509Certificate2 certificate);

        Task<Device> CreateDeviceAsync(Device device);
        Task<Device> UpdateDeviceAsync(Device device);
        Task<Device> GetDeviceAsync(string deviceId);
        Task<IList<Device>> FindDevices(string name);
    }
}