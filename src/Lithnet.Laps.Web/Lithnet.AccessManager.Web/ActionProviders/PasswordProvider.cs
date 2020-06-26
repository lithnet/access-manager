using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Web
{
    public class PasswordProvider : IPasswordProvider
    {
        private readonly IMsMcsAdmPwdProvider msLapsProvider;

        private readonly IAppDataProvider appDataProvider;

        private readonly IEncryptionProvider encryptionProvider;

        private readonly ICertificateResolver certificateResolver;

        public PasswordProvider(IMsMcsAdmPwdProvider msMcsAdmPwdProvider, IAppDataProvider appDataProvider, IEncryptionProvider encryptionProvider, ICertificateResolver certificateResolver)
        {
            this.msLapsProvider = msMcsAdmPwdProvider;
            this.appDataProvider = appDataProvider;
            this.encryptionProvider = encryptionProvider;
            this.certificateResolver = certificateResolver;
        }

        public IList<PasswordEntry> GetPasswordEntries(IComputer computer, DateTime? newExpiry, bool getHistory)
        {
            if (this.appDataProvider.TryGetAppData(computer, out IAppData data) && data.CurrentPassword != null)
            {
                return this.GetAppDataEntries(data, newExpiry, getHistory);
            }
            else
            {
                return this.GetLapsEntry(computer, newExpiry);
            }
        }

        private IList<PasswordEntry> GetAppDataEntries(IAppData data, DateTime? newExpiry, bool getHistory)
        {
            List<PasswordEntry> list = new List<PasswordEntry>();

            PasswordEntry current = new PasswordEntry()
            {
                IsCurrent = true,
                Created = data.CurrentPassword.Created,
                Password = this.encryptionProvider.Decrypt(data.CurrentPassword.EncryptedData, this.certificateResolver.GetCertificateWithPrivateKey),
                ExpiryDate = newExpiry ?? data.PasswordExpiry
            };
             
            list.Add(current);

            if (getHistory)
            {
                foreach (var item in data.PasswordHistory.Where(t => t.Retired != null))
                {
                    PasswordEntry p = new PasswordEntry()
                    {
                        IsCurrent = false,
                        Created = item.Created,
                        Password = this.encryptionProvider.Decrypt(item.EncryptedData, this.certificateResolver.GetCertificateWithPrivateKey),
                        ExpiryDate = item.Retired
                    };

                    list.Add(p);
                }
            }

            if (newExpiry != null)
            {
                data.UpdatePasswordExpiry(newExpiry.Value);
            }

            return list;
        }

        private IList<PasswordEntry> GetLapsEntry(IComputer computer, DateTime? newExpiry)
        {
            List<PasswordEntry> list = new List<PasswordEntry>();

            var result = this.msLapsProvider.GetPassword(computer, newExpiry);

            if (string.IsNullOrWhiteSpace(result.Password))
            {
                return list;
            }

            PasswordEntry current = new PasswordEntry()
            {
                IsCurrent = true,
                Created = null,
                Password = result.Password,
                ExpiryDate = newExpiry ?? result.ExpiryDate
            };

            list.Add(current);

            return list;
        }
    }
}
