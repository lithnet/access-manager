using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Web
{
    public class PasswordProvider : IPasswordProvider
    {
        public IList<PasswordEntry> GetPasswordEntries(IComputer computer, TimeSpan? expireAfter, bool getHistory)
        {
            return new List<PasswordEntry> { new PasswordEntry { Password = "this is a test", ExpiryDate = DateTime.Now, IsCurrent = true } };
        }
    }
}
