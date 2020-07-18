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

        private readonly ILogger<LithnetAdminPasswordProvider> logger;

        public LithnetAdminPasswordProvider(ILogger<LithnetAdminPasswordProvider> logger)
        {
            this.logger = logger;
        }

        public ProtectedPasswordHistoryItem GetCurrentPassword(IComputer computer, DateTime? newExpiry)
        {
            DirectoryEntry de = computer.DirectoryEntry;

            var data = this.GetCurrentPassword(de);

            if (newExpiry != null)
            {
                de.Properties["lithnetAdminPasswordExpiry"].Value = newExpiry.Value.ToFileTimeUtc().ToString();
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
            List<string> items = de.GetPropertyStrings("lithnetAdminPasswordHistory").ToList();

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
            de.RefreshCache();

            return de.GetPropertyDateTimeFromAdsLargeInteger("lithnetAdminPasswordExpiry");
        }

        public void UpdatePasswordExpiry(IComputer computer, DateTime expiry)
        {
            DirectoryEntry de = computer.DirectoryEntry;
            de.Properties["lithnetAdminPasswordExpiry"].Value = expiry.ToFileTimeUtc().ToString();
            de.CommitChanges();
        }

        public void UpdateCurrentPassword(IComputer computer, string encryptedPassword, DateTime rotationInstant, DateTime expiryDate, int maximumPasswordHistory)
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
                EncryptedData = encryptedPassword,
            };

            de.Properties["lithnetAdminPasswordHistory"].Clear();
            if (items.Count > 0)
            {
                de.Properties["lithnetAdminPasswordHistory"]
                    .AddRange(items.Select(JsonConvert.SerializeObject).ToArray<object>());
            }

            de.Properties["lithnetAdminPasswordExpiry"].Value = expiryDate.ToFileTimeUtc().ToString();
            de.Properties["lithnetAdminPassword"].Value = JsonConvert.SerializeObject(newPassword);
            de.CommitChanges();
        }

        private ProtectedPasswordHistoryItem GetCurrentPassword(DirectoryEntry de)
        {
            string rawExistingPassword = de.GetPropertyString("lithnetAdminPassword");

            if (!string.IsNullOrWhiteSpace(rawExistingPassword))
            {
                return JsonConvert.DeserializeObject<ProtectedPasswordHistoryItem>(rawExistingPassword);
            }

            return null;
        }

        public void ClearPasswordHistory(IComputer computer)
        {
            DirectoryEntry de = computer.DirectoryEntry;

            de.Properties["lithnetAdminPasswordHistory"].Clear();
            de.CommitChanges();
        }

        public void ClearPassword(IComputer computer)
        {
            DirectoryEntry de = computer.DirectoryEntry;

            de.Properties["lithnetAdminPassword"].Clear();
            de.Properties["lithnetAdminPasswordExpiry"].Clear();
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