using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public class PasswordProvider : IPasswordProvider
    {
        private readonly IMsMcsAdmPwdProvider msLapsProvider;

        private readonly ILithnetAdminPasswordProvider lithnetProvider;

        private readonly IEncryptionProvider encryptionProvider;

        private readonly ICertificateProvider certificateProvider;

        public PasswordProvider(IMsMcsAdmPwdProvider msMcsAdmPwdProvider, ILithnetAdminPasswordProvider lithnetProvider, IEncryptionProvider encryptionProvider, ICertificateProvider certificateProvider)
        {
            this.msLapsProvider = msMcsAdmPwdProvider;
            this.lithnetProvider = lithnetProvider;
            this.encryptionProvider = encryptionProvider;
            this.certificateProvider = certificateProvider;
        }

        public PasswordEntry GetCurrentPassword(IComputer computer, DateTime? newExpiry, PasswordStorageLocation retrievalLocation)
        {
            if (retrievalLocation == 0 || (retrievalLocation.HasFlag(PasswordStorageLocation.MsLapsAttribute) && retrievalLocation.HasFlag(PasswordStorageLocation.LithnetAttribute)))
            {
                return GetFromMsLapsOrLithnet(computer, newExpiry) ?? throw new NoPasswordException();
            }
            else if (retrievalLocation.HasFlag(PasswordStorageLocation.MsLapsAttribute))
            {
                return this.GetMsLapsEntry(computer, newExpiry) ?? throw new NoPasswordException();
            }
            else
            {
                return this.GetLithnetCurrentPassword(computer, newExpiry) ?? throw new NoPasswordException();
            }
        }

        public IList<PasswordEntry> GetPasswordHistory(IComputer computer)
        {
            return this.GetPasswordHistoryEntries(computer) ?? throw new NoPasswordException();
        }

        private PasswordEntry GetFromMsLapsOrLithnet(IComputer computer, DateTime? newExpiry)
        {
            var result = this.GetLithnetCurrentPassword(computer, newExpiry);

            if (result == null)
            {
                return this.GetMsLapsEntry(computer, newExpiry);
            }
            else
            {
                return result;
            }
        }

        private PasswordEntry GetLithnetCurrentPassword(IComputer computer, DateTime? newExpiry)
        {
            var item = this.lithnetProvider.GetCurrentPassword(computer, newExpiry);

            if (item == null)
            {
                return null;
            }

            PasswordEntry current = new PasswordEntry()
            {
                Created = item.Created,
                Password = this.encryptionProvider.Decrypt(item.EncryptedData, this.certificateProvider.FindDecryptionCertificate),
                ExpiryDate = newExpiry ?? this.lithnetProvider.GetExpiry(computer)
            };

            return current;
        }

        private IList<PasswordEntry> GetPasswordHistoryEntries(IComputer computer)
        {
            List<PasswordEntry> list = new List<PasswordEntry>();

            foreach (var item in this.lithnetProvider.GetPasswordHistory(computer))
            {
                PasswordEntry p = new PasswordEntry()
                {
                    Created = item.Created,
                    Password = this.encryptionProvider.Decrypt(item.EncryptedData, this.certificateProvider.FindDecryptionCertificate),
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
                return null;
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
