using System;
using System.Collections.Generic;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public interface IPasswordProvider
    {
        IList<PasswordEntry> GetPasswordEntries(IComputer computer, TimeSpan? expireAfter);
    }
}