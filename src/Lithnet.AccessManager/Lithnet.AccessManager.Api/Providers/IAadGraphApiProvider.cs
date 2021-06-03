using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface IAadGraphApiProvider
    {
        Task<Microsoft.Graph.Device> GetAadDeviceAsync(string deviceId);
    }
}