using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Lithnet.AccessManager
{
    public class LithnetAdminPasswordProvider : ILithnetAdminPasswordProvider
    {
        private const int PasswordHistoryItemLimit = 500;

        private const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";
        private const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";
        private const string AttrLithnetAdminPasswordHistory = "lithnetAdminPasswordHistory";
        private const string AttrLithnetAdminPasswordExpiry = "lithnetAdminPasswordExpiry";
        private const string AttrLithnetAdminPassword = "lithnetAdminPassword";

        private readonly ILogger<LithnetAdminPasswordProvider> logger;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly ICertificateProvider certificateProvider;

        public LithnetAdminPasswordProvider(ILogger<LithnetAdminPasswordProvider> logger, IEncryptionProvider encryptionProvider, ICertificateProvider certificateProvider)
        {
            this.logger = logger;
            this.encryptionProvider = encryptionProvider;
            this.certificateProvider = certificateProvider;
        }

        public ProtectedPasswordHistoryItem GetCurrentPassword(IComputer computer, DateTime? newExpiry)
        {
            DirectoryEntry de = computer.DirectoryEntry;

            var data = this.GetCurrentPassword(de);

            if (data == null)
            {
                return null;
            }

            if (newExpiry != null)
            {
                de.Properties[AttrLithnetAdminPasswordExpiry].Value = newExpiry.Value.ToFileTimeUtc().ToString();
                de.CommitChanges();
            }

            return data;
        }

        public IReadOnlyList<ProtectedPasswordHistoryItem> GetPasswordHistory(IComputer computer)
        {
            DirectoryEntry de = computer.DirectoryEntry;
            List<ProtectedPasswordHistoryItem> list = GetPasswordHistory(de);
            return list.AsReadOnly();
        }

        private List<ProtectedPasswordHistoryItem> GetPasswordHistory(DirectoryEntry de)
        {
            List<string> items = de.GetPropertyStrings(AttrLithnetAdminPasswordHistory).ToList();

            List<ProtectedPasswordHistoryItem> list = new List<ProtectedPasswordHistoryItem>();

            foreach (string item in items)
            {
                list.Add(JsonConvert.DeserializeObject<ProtectedPasswordHistoryItem>(item));
            }

            return list;
        }

        public DateTime? GetExpiry(IComputer computer)
        {
            DirectoryEntry de = computer.DirectoryEntry;
            de.RefreshCache(ActiveDirectoryComputer.PropertiesToGet);

            return de.GetPropertyDateTimeFromAdsLargeInteger(AttrLithnetAdminPasswordExpiry);
        }

        private DateTime? GetMsMcsAdmPwdExpiry(IComputer computer)
        {
            DirectoryEntry de = computer.DirectoryEntry;
            de.RefreshCache(ActiveDirectoryComputer.PropertiesToGet);

            return de.GetPropertyDateTimeFromAdsLargeInteger(AttrMsMcsAdmPwdExpirationTime);
        }


        public void UpdatePasswordExpiry(IComputer computer, DateTime expiry)
        {
            DirectoryEntry de = computer.DirectoryEntry;
            de.Properties[AttrLithnetAdminPasswordExpiry].Value = expiry.ToFileTimeUtc().ToString();
            de.CommitChanges();
        }

        public void UpdateCurrentPassword(IComputer computer, string password, DateTime rotationInstant, DateTime expiryDate, int maximumPasswordHistory, PasswordAttributeBehaviour msLapsBehaviour)
        {
            DirectoryEntry de = computer.DirectoryEntry;

            ProtectedPasswordHistoryItem oldPassword = GetCurrentPassword(de);
            if (oldPassword != null)
            {
                oldPassword.Retired = rotationInstant;
            }

            List<ProtectedPasswordHistoryItem> items = this.GetPasswordHistory(de);
            if (oldPassword != null)
            {
                items.Insert(0, oldPassword);
            }

            items = this.PruneHistoryItems(items, maximumPasswordHistory);

            ProtectedPasswordHistoryItem newPassword = new ProtectedPasswordHistoryItem()
            {
                Created = rotationInstant,
                EncryptedData = this.encryptionProvider.Encrypt(this.certificateProvider.FindEncryptionCertificate(), password)
            };

            de.Properties[AttrLithnetAdminPasswordHistory].Clear();
            if (items.Count > 0)
            {
                de.Properties[AttrLithnetAdminPasswordHistory]
                    .AddRange(items.Select(JsonConvert.SerializeObject).ToArray<object>());
            }

            de.Properties[AttrLithnetAdminPasswordExpiry].Value = expiryDate.ToFileTimeUtc().ToString();
            de.Properties[AttrLithnetAdminPassword].Value = JsonConvert.SerializeObject(newPassword);

            if (msLapsBehaviour == PasswordAttributeBehaviour.Populate)
            {
                de.Properties[AttrMsMcsAdmPwd].Value = password;
                de.Properties[AttrMsMcsAdmPwdExpirationTime].Value = expiryDate.ToFileTimeUtc().ToString();
            }
            else if (msLapsBehaviour == PasswordAttributeBehaviour.Clear)
            {
                de.Properties[AttrMsMcsAdmPwd].Clear();
                de.Properties[AttrMsMcsAdmPwdExpirationTime].Clear();
            }

            de.CommitChanges();
        }

        public bool HasPasswordExpired(IComputer computer, bool considerMsMcsAdmPwdExpiry)
        {
            DateTime? lithnetExpiry = this.GetExpiry(computer);

            if (lithnetExpiry == null)
            {
                return true;
            }

            if (DateTime.UtcNow > lithnetExpiry)
            {
                return true;
            }

            if (considerMsMcsAdmPwdExpiry)
            {
                var lapsExpiry = this.GetMsMcsAdmPwdExpiry(computer);

                if (lapsExpiry == null)
                {
                    return true;
                }

                return DateTime.UtcNow > lapsExpiry;
            }

            return false;
        }

        private ProtectedPasswordHistoryItem GetCurrentPassword(DirectoryEntry de)
        {
            string rawExistingPassword = de.GetPropertyString(AttrLithnetAdminPassword);

            if (!string.IsNullOrWhiteSpace(rawExistingPassword))
            {
                return JsonConvert.DeserializeObject<ProtectedPasswordHistoryItem>(rawExistingPassword);
            }

            return null;
        }

        public void ClearPasswordHistory(IComputer computer)
        {
            DirectoryEntry de = computer.DirectoryEntry;

            de.Properties[AttrLithnetAdminPasswordHistory].Clear();
            de.CommitChanges();
        }

        public void ClearPassword(IComputer computer)
        {
            DirectoryEntry de = computer.DirectoryEntry;

            de.Properties[AttrLithnetAdminPassword].Clear();
            de.Properties[AttrLithnetAdminPasswordExpiry].Clear();
            de.CommitChanges();
        }

        internal List<ProtectedPasswordHistoryItem> PruneHistoryItems(IEnumerable<ProtectedPasswordHistoryItem> items, int maximumPasswordHistoryDays)
        {
            List<ProtectedPasswordHistoryItem> newItems = new List<ProtectedPasswordHistoryItem>();

            if (maximumPasswordHistoryDays <= 0)
            {
                return newItems;
            }

            foreach (ProtectedPasswordHistoryItem item in items.OrderByDescending(t => t.Created))
            {
                if (item.Retired == null || item.Retired.Value.AddDays(maximumPasswordHistoryDays) > DateTime.UtcNow)
                {
                    newItems.Add(item);
                }

                if (newItems.Count >= PasswordHistoryItemLimit)
                {
                    break;
                }
            }

            return newItems;
        }
    }
}