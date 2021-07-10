using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public interface IDeviceProvider
    {
        Task<IDevice> GetOrCreateDeviceAsync(Microsoft.Graph.Device device, string authority);

        Task<IDevice> GetOrCreateDeviceAsync(IActiveDirectoryComputer principal, string authority);

        Task<IDevice> CreateDeviceAsync(IActiveDirectoryComputer computer, string authority, string deviceId);

        Task<IDevice> GetDeviceAsync(AuthorityType authorityType, string authority, string authorityDeviceId);

        Task<IDevice> GetDeviceAsync(X509Certificate2 certificate);

        Task<IDevice> CreateDeviceAsync(IDevice device, X509Certificate2 certificate);

        Task<IDevice> CreateDeviceAsync(IDevice device);
        
        Task<IDevice> UpdateDeviceAsync(IDevice device);
        
        Task<IDevice> GetDeviceAsync(string deviceId);

        Task<IList<IDevice>> FindDevices(string name);

        IAsyncEnumerable<IDevice> GetDevices(int startIndex, int count);

        Task ApproveDevice(string deviceId);

        Task DeleteDevice(string deviceId);

        Task RejectDevice(string deviceId);

        Task AddDeviceCredentialsAsync(IDevice device, X509Certificate2 certificate);
        Task DisableDevice(string deviceId);
        Task EnableDevice(string deviceId);
    }
}