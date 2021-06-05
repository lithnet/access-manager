using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface IDbDevicePasswordProvider
    {
        Task<string> UpdateDevicePassword(string deviceId, PasswordUpdateRequest request);
        
        Task RevertLastPasswordChange(string deviceId, string requestId);
        
        Task<bool> HasPasswordExpired(string deviceId);
    }
}