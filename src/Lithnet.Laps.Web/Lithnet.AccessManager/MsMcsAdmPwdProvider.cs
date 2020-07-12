using System;
using System.DirectoryServices;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public class MsMcsAdmPwdProvider : IMsMcsAdmPwdProvider
    {
        private readonly ILogger<MsMcsAdmPwdProvider> logger;

        private const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";

        private const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";

        public MsMcsAdmPwdProvider(ILogger<MsMcsAdmPwdProvider> logger)
        {
            this.logger = logger;
        }

        public void SetPassword(IComputer computer, string password, DateTime expiryDate)
        {
            DirectoryEntry de = computer.GetDirectoryEntry();
            de.Properties[AttrMsMcsAdmPwd].Value = password;
            de.Properties[AttrMsMcsAdmPwdExpirationTime].Value = expiryDate.ToFileTimeUtc().ToString();
            de.CommitChanges();
        }

        public void ClearPassword(IComputer computer)
        {
            DirectoryEntry de = computer.GetDirectoryEntry();

            if (de.Properties.Contains(AttrMsMcsAdmPwd) || de.Properties.Contains(AttrMsMcsAdmPwdExpirationTime))
            {
                de.Properties[AttrMsMcsAdmPwd].Clear();
                de.Properties[AttrMsMcsAdmPwdExpirationTime].Clear();
                de.CommitChanges();
            }    
        }

        public MsMcsAdmPwdPassword GetPassword(IComputer computer, DateTime? newExpiry)
        {
            DirectoryEntry de = computer.GetDirectoryEntry();

            if (!de.Properties.Contains(AttrMsMcsAdmPwd))
            {
                throw new NoPasswordException();
            }

            MsMcsAdmPwdPassword p = new MsMcsAdmPwdPassword()
            {
                Password = de.GetPropertyString(AttrMsMcsAdmPwd)
            };

            if (newExpiry != null)
            {
                p.ExpiryDate = newExpiry.Value;

                de.Properties[AttrMsMcsAdmPwdExpirationTime].Value = p.ExpiryDate.Value.ToFileTimeUtc().ToString();
                de.CommitChanges();

                this.logger.LogTrace($"Set expiry time for {computer.MsDsPrincipalName} to {p.ExpiryDate.Value.ToLocalTime()}");
            }
            else
            {
                p.ExpiryDate = de.GetPropertyDateTimeFromAdsLargeInteger(AttrMsMcsAdmPwdExpirationTime);
            }

            return p;
        }

        public DateTime? GetExpiry(IComputer computer)
        {
            DirectoryEntry de = computer.GetDirectoryEntry();

            if (!de.Properties.Contains(AttrMsMcsAdmPwdExpirationTime))
            {
                return null;
            }

            return de.GetPropertyDateTimeFromAdsLargeInteger(AttrMsMcsAdmPwdExpirationTime);
        }
    }
}