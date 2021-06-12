using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface IDbDevicePasswordProvider
    {
        Task<string> UpdateDevicePassword(string deviceId, PasswordUpdateRequest request);
        
        Task RevertLastPasswordChange(string deviceId, string requestId);
        
        Task<bool> HasPasswordExpired(string deviceId);
    }
}