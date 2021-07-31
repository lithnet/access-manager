using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lithnet.AccessManager.Cryptography;
using Lithnet.AccessManager.Enterprise;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server
{
    public class PasswordProvider : IPasswordProvider
    {
        private readonly IMsMcsAdmPwdProvider msLapsProvider;
        private readonly ILithnetAdminPasswordProvider lithnetProvider;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly ICertificateProvider certificateProvider;
        private readonly ILogger logger;
        private readonly IDevicePasswordProvider devicePasswordProvider;
        private readonly IAmsLicenseManager licenseManager;

        public PasswordProvider(IMsMcsAdmPwdProvider msMcsAdmPwdProvider, ILithnetAdminPasswordProvider lithnetProvider, IEncryptionProvider encryptionProvider, ICertificateProvider certificateProvider, ILogger<PasswordProvider> logger, IDevicePasswordProvider devicePasswordProvider, IAmsLicenseManager licenseManager)
        {
            this.msLapsProvider = msMcsAdmPwdProvider;
            this.lithnetProvider = lithnetProvider;
            this.encryptionProvider = encryptionProvider;
            this.certificateProvider = certificateProvider;
            this.logger = logger;
            this.devicePasswordProvider = devicePasswordProvider;
            this.licenseManager = licenseManager;
        }

        public async Task<PasswordEntry> GetCurrentPassword(IComputer computer, DateTime? newExpiry, PasswordStorageLocation retrievalLocation)
        {
            switch (computer)
            {
                case IActiveDirectoryComputer adComputer:
                    return this.GetCurrentPasswordFromActiveDirectory(adComputer, newExpiry, retrievalLocation);

                case IDevice device:
                    return await this.GetCurrentPasswordFromDatabase(device, newExpiry);

                default:
                    throw new InvalidOperationException("The computer object type supplied is not known");
            }
        }

        public async Task<IList<PasswordEntry>> GetPasswordHistory(IComputer computer)
        {
            switch (computer)
            {
                case IActiveDirectoryComputer adComputer:
                    return this.GetPasswordHistoryFromActiveDirectory(adComputer);

                case IDevice device:
                    return await this.GetPasswordHistoryFromDatabase(device);

                default:
                    throw new InvalidOperationException("The computer object type supplied is not known");
            }
        }

        private async Task<PasswordEntry> GetCurrentPasswordFromDatabase(IDevice device, DateTime? newExpiry)
        {
            var password = newExpiry == null ?
                await this.devicePasswordProvider.GetCurrentPassword(device.ObjectID) :
                await this.devicePasswordProvider.GetCurrentPassword(device.ObjectID, newExpiry.Value);

            return new PasswordEntry
            {
                Created = password.EffectiveDate.ToLocalTime(),
                AccountName = this.licenseManager.IsFeatureEnabled(LicensedFeatures.LapsAccountNameDisplay) ? password.AccountName : null,
                ExpiryDate = password.ExpiryDate.ToLocalTime(),
                Password = this.encryptionProvider.Decrypt(password.PasswordData, (thumbprint) => this.certificateProvider.FindDecryptionCertificate(thumbprint))
            };
        }

        private async Task<IList<PasswordEntry>> GetPasswordHistoryFromDatabase(IDevice device)
        {
            var passwords = await this.devicePasswordProvider.GetPasswordHistory(device.ObjectID);

            List<PasswordEntry> list = new List<PasswordEntry>();

            foreach (var item in passwords)
            {
                PasswordEntry p = new PasswordEntry()
                {
                    AccountName = this.licenseManager.IsFeatureEnabled(LicensedFeatures.LapsAccountNameDisplay) ? item.AccountName : null,
                    Created = item.EffectiveDate.ToLocalTime(),
                    ExpiryDate = item.ExpiryDate.ToLocalTime()
                };

                string tp = null;

                try
                {
                    p.Password = this.encryptionProvider.Decrypt(item.PasswordData,
                        (thumbprint) =>
                        {
                            tp = thumbprint;
                            return this.certificateProvider.FindDecryptionCertificate(thumbprint);
                        });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.LapsPasswordHistoryError, ex, $"Could not decrypt a password history item. Certificate thumbprint {tp}, Created: {p.Created?.ToLocalTime()}, Expired: {p.ExpiryDate?.ToLocalTime()}");
                    p.DecryptionFailed = true;
                }

                list.Add(p);
            }

            if (list.Count == 0)
            {
                throw new NoPasswordException();
            }

            return list;
        }

        private PasswordEntry GetCurrentPasswordFromActiveDirectory(IActiveDirectoryComputer computer, DateTime? newExpiry, PasswordStorageLocation retrievalLocation)
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


        private IList<PasswordEntry> GetPasswordHistoryFromActiveDirectory(IActiveDirectoryComputer computer)
        {
            return this.GetPasswordHistoryEntries(computer) ?? throw new NoPasswordException();
        }

        private PasswordEntry GetFromMsLapsOrLithnet(IActiveDirectoryComputer computer, DateTime? newExpiry)
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

        private PasswordEntry GetLithnetCurrentPassword(IActiveDirectoryComputer computer, DateTime? newExpiry)
        {
            var item = this.lithnetProvider.GetCurrentPassword(computer, newExpiry);

            if (item == null)
            {
                return null;
            }

            PasswordEntry current = new PasswordEntry()
            {
                AccountName = this.licenseManager.IsFeatureEnabled(LicensedFeatures.LapsAccountNameDisplay) ? item.AccountName : null,
                Created = item.Created.ToLocalTime(),
                Password = this.encryptionProvider.Decrypt(item.EncryptedData, (thumbprint) => this.certificateProvider.FindDecryptionCertificate(thumbprint)),
                ExpiryDate = newExpiry?.ToLocalTime() ?? this.lithnetProvider.GetExpiry(computer)?.ToLocalTime()
            };

            return current;
        }

        private IList<PasswordEntry> GetPasswordHistoryEntries(IActiveDirectoryComputer computer)
        {
            List<PasswordEntry> list = new List<PasswordEntry>();

            foreach (var item in this.lithnetProvider.GetPasswordHistory(computer))
            {
                PasswordEntry p = new PasswordEntry()
                {
                    AccountName = this.licenseManager.IsFeatureEnabled(LicensedFeatures.LapsAccountNameDisplay) ? item.AccountName : null,
                    Created = item.Created.ToLocalTime(),
                    ExpiryDate = item.Retired?.ToLocalTime()
                };

                string tp = null;

                try
                {

                    p.Password = this.encryptionProvider.Decrypt(item.EncryptedData,
                        (thumbprint) =>
                        {
                            tp = thumbprint;
                            return this.certificateProvider.FindDecryptionCertificate(thumbprint);
                        });
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.LapsPasswordHistoryError, ex, $"Could not decrypt a password history item. Certificate thumbprint {tp}, Created: {p.Created?.ToLocalTime()}, Expired: {p.ExpiryDate?.ToLocalTime()}");
                    p.DecryptionFailed = true;
                }

                list.Add(p);
            }

            if (list.Count == 0)
            {
                throw new NoPasswordException();
            }

            return list;
        }

        private PasswordEntry GetMsLapsEntry(IActiveDirectoryComputer computer, DateTime? newExpiry)
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
                ExpiryDate = newExpiry?.ToLocalTime() ?? result.ExpiryDate?.ToLocalTime()
            };
        }
    }
}
