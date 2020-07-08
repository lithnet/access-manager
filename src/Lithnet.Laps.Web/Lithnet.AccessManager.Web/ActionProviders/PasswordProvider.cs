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

        private readonly ICertificateProvider certificateProvider;

        public PasswordProvider(IMsMcsAdmPwdProvider msMcsAdmPwdProvider, IAppDataProvider appDataProvider, IEncryptionProvider encryptionProvider, ICertificateProvider certificateProvider)
        {
            this.msLapsProvider = msMcsAdmPwdProvider;
            this.appDataProvider = appDataProvider;
            this.encryptionProvider = encryptionProvider;
            this.certificateProvider = certificateProvider;
        }

        public PasswordEntry GetCurrentPassword(IComputer computer, DateTime? newExpiry, PasswordStorageLocation retrievalLocation)
        {
            if (retrievalLocation == 0 || (retrievalLocation.HasFlag(PasswordStorageLocation.MsLapsAttribute) && retrievalLocation.HasFlag(PasswordStorageLocation.LithnetAttribute)))
            {
                return GetFromMsLapsOrAppData(computer, newExpiry);
            }
            else if (retrievalLocation.HasFlag(PasswordStorageLocation.MsLapsAttribute))
            {
                return this.GetMsLapsEntry(computer, newExpiry);
            }
            else
            {
                if (this.appDataProvider.TryGetAppData(computer, out IAppData data))
                {
                    return this.GetAppDataCurrentPassword(data, newExpiry);
                }
            }

            throw new NoPasswordException();
        }

        public IList<PasswordEntry> GetPasswordHistory(IComputer computer)
        {
            if (this.appDataProvider.TryGetAppData(computer, out IAppData data))
            {
                return GetAppDataPasswordHistoryEntries(data);
            }

            throw new NoPasswordException();
        }

        private PasswordEntry GetFromMsLapsOrAppData(IComputer computer, DateTime? newExpiry)
        {
            if (this.appDataProvider.TryGetAppData(computer, out IAppData data) && data.CurrentPassword != null)
            {
                return this.GetAppDataCurrentPassword(data, newExpiry);
            }
            else
            {
                return this.GetMsLapsEntry(computer, newExpiry);
            }
        }

        private PasswordEntry GetAppDataCurrentPassword(IAppData data, DateTime? newExpiry)
        {
            if (data.CurrentPassword == null)
            {
                throw new NoPasswordException();
            }

            PasswordEntry current = new PasswordEntry()
            {
                Created = data.CurrentPassword.Created,
                Password = this.encryptionProvider.Decrypt(data.CurrentPassword.EncryptedData, this.certificateProvider.GetCertificateWithPrivateKey),
                ExpiryDate = newExpiry ?? data.PasswordExpiry
            };

            if (newExpiry != null)
            {
                data.UpdatePasswordExpiry(newExpiry.Value);
            }

            return current;
        }

        private IList<PasswordEntry> GetAppDataPasswordHistoryEntries(IAppData data)
        {
            List<PasswordEntry> list = new List<PasswordEntry>();

            foreach (var item in data.PasswordHistory.Where(t => t.Retired != null))
            {
                PasswordEntry p = new PasswordEntry()
                {
                    Created = item.Created,
                    Password = this.encryptionProvider.Decrypt(item.EncryptedData, this.certificateProvider.GetCertificateWithPrivateKey),
                    ExpiryDate = item.Retired
                };

                list.Add(p);
            }

            if (list.Count == 0)
            {
                throw new NoPasswordException();
            }

            return list;
        }

        private PasswordEntry GetMsLapsEntry(IComputer computer, DateTime? newExpiry)
        {
            var result = this.msLapsProvider.GetPassword(computer, newExpiry);

            if (string.IsNullOrWhiteSpace(result.Password))
            {
                throw new NoPasswordException();
            }

            return new PasswordEntry()
            {
                Created = null,
                Password = result.Password,
                ExpiryDate = newExpiry ?? result.ExpiryDate
            };
        }
    }
}
