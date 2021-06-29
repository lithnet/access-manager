using Lithnet.AccessManager.Api.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public interface IDevicePasswordProvider
    {
        Task<string> UpdateDevicePassword(string deviceId, PasswordUpdateRequest request);

        Task RevertLastPasswordChange(string deviceId, string requestId);

        Task<bool> HasPasswordExpired(string deviceId);

        Task<IPasswordData> GetCurrentPassword(string deviceId);

        Task<IPasswordData> GetCurrentPassword(string deviceId, DateTime newExpiry);

        Task<IList<IPasswordData>> GetPasswordHistory(string deviceId);
    }
}