using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface IPasswordProvider
    {
        PasswordEntry GetCurrentPassword(IComputer computer, DateTime? newExpiry, PasswordStorageLocation retrievalLocation);

        IList<PasswordEntry> GetPasswordHistory(IComputer computer);
    }
}