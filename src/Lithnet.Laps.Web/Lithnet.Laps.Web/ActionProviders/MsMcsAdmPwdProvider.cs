using Lithnet.Laps.Web.Internal;
using NLog;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public class MsMcsAdmPwdProvider : IPasswordProvider
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        private const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";

        private const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";

        public MsMcsAdmPwdProvider(IDirectory directory, ILogger logger)
        {
            this.directory = directory;
            this.logger = logger;
        }

        public IList<PasswordEntry> GetPasswordEntries(IComputer computer, TimeSpan? expireAfter)
        {
            SearchResult searchResult = this.directory.GetDirectoryEntry(computer.DistinguishedName, "computer", AttrMsMcsAdmPwd, AttrMsMcsAdmPwdExpirationTime);

            if (!searchResult.Properties.Contains(AttrMsMcsAdmPwd))
            {
                throw new NoPasswordException();
            }

            PasswordEntry p = new PasswordEntry();
            p.Password = searchResult.GetPropertyString(AttrMsMcsAdmPwd);

            if (expireAfter != null && expireAfter.Value.Ticks > 0)
            {
                this.logger.Trace($"Target rule requires password to change after {expireAfter}");

                DirectoryEntry entry = searchResult.GetDirectoryEntry();
                p.ExpiryDate = DateTime.UtcNow.Add(expireAfter.Value);
                entry.Properties[AttrMsMcsAdmPwdExpirationTime].Value = p.ExpiryDate.Value.ToFileTimeUtc().ToString();
                entry.CommitChanges();

                this.logger.Trace($"Set expiry time for {computer.MsDsPrincipalName} to {p.ExpiryDate.Value.ToLocalTime()}");
            }
            else
            {
                p.ExpiryDate = searchResult.GetPropertyDateTimeFromLong(AttrMsMcsAdmPwdExpirationTime);
            }

            return new List<PasswordEntry> { p };
        }
    }
}
