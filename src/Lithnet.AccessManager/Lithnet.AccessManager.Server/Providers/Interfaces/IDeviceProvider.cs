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
        Task AddDeviceCredentialsAsync(IDevice device, X509Certificate2 certificate);
    }
}