using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager.Web
{
    public interface IPasswordProvider
    {
        PasswordEntry GetCurrentPassword(IComputer computer, DateTime? newExpiry, PasswordStorageLocation retrievalLocation);

        IList<PasswordEntry> GetPasswordHistory(IComputer computer);
    }
}