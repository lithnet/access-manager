using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent
{
    public interface IPasswordStorageProvider
    {
        Task<bool> IsPasswordChangeRequired();

        Task UpdatePassword(string accountName, string password, DateTime expiry);

        Task RollbackPasswordUpdate();

        Task Commit();

        IPasswordPolicy GetPolicy();
    }
}