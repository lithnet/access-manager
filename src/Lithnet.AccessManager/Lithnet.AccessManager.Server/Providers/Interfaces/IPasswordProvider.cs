using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    public interface IPasswordProvider
    {
        Task<PasswordEntry> GetCurrentPassword(IComputer computer, DateTime? newExpiry, PasswordStorageLocation retrievalLocation);

        Task<IList<PasswordEntry>> GetPasswordHistory(IComputer computer);
    }
}