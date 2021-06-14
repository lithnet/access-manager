using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface IDeviceProvider
    {
        Task<Device> GetOrCreateDeviceAsync(Microsoft.Graph.Device device, string authority);

        Task<Device> GetOrCreateDeviceAsync(IComputer principal, string authority);

        Task<Device> CreateDeviceAsync(IComputer computer, string authority, string deviceId);

        Task<Device> GetDeviceAsync(AuthorityType authorityType, string authority, string authorityDeviceId);

        Task<Device> GetDeviceAsync(X509Certificate2 certificate);

        Task<Device> CreateDeviceAsync(Device device, X509Certificate2 certificate);

        Task<Device> CreateDeviceAsync(Device device);
        Task<Device> UpdateDeviceAsync(Device device);
        Task<Device> GetDeviceAsync(string deviceId);
    }
}