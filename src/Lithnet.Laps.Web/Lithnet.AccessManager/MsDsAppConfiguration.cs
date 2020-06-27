using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using Newtonsoft.Json;

namespace Lithnet.AccessManager
{
    public sealed class MsDsAppConfiguration : IAppData
    {
        private IReadOnlyList<ProtectedPasswordHistoryItem> passwordHistoryEntries;

        private readonly DirectoryEntry de;

        private const int PasswordHistoryItemLimit = 500;

        public MsDsAppConfiguration(DirectoryEntry de)
        {
            this.de = de;
        }

        public MsDsAppConfiguration(SearchResult sr) : this(sr.GetDirectoryEntry())
        {
        }

        public DateTime? PasswordExpiry => this.de?.GetPropertyDateTime("msDS-DateTime");

        public int? Flags => this.de?.GetPropertyInteger("msDS-Integer");

        public string DistinguishedName => this.de?.GetPropertyString("distinguishedName");

        public IReadOnlyList<ProtectedPasswordHistoryItem> PasswordHistory
        {
            get
            {
                if (this.passwordHistoryEntries == null)
                {
                    List<ProtectedPasswordHistoryItem> list = new List<ProtectedPasswordHistoryItem>();

                    foreach (string item in this.PasswordHistoryData)
                    {
                        list.Add(JsonConvert.DeserializeObject<ProtectedPasswordHistoryItem>(item));
                    }

                    this.passwordHistoryEntries = list.AsReadOnly();
                }

                return this.passwordHistoryEntries;
            }
        }

        public IEnumerable<string> PasswordHistoryData => this.de?.GetPropertyStrings("msDS-Settings");

        public Guid? Guid => this.de?.GetPropertyGuid("objectGuid");

        public ProtectedPasswordHistoryItem CurrentPassword
        {
            get
            {
                var items = this.PasswordHistory.Where(t => t.Retired == null).ToList();

                if (items.Count == 0)
                {
                    return null;
                }

                if (items.Count > 1)
                {
                    throw new AccessManagerException("There were multiple active passwords in the directory");
                }

                return items[0];
            }
        }

        public void UpdatePasswordExpiry(DateTime newExpiry)
        {
            this.de.Properties["msDS-DateTime"].Value = newExpiry;
            this.de.CommitChanges();
        }

        public void UpdateCurrentPassword(string encryptedPassword, DateTime rotationInstant, DateTime expiryDate, int maximumPasswordHistory)
        {
            ProtectedPasswordHistoryItem pphi = new ProtectedPasswordHistoryItem()
            {
                Created = rotationInstant,
                EncryptedData = encryptedPassword,
            };

            List<ProtectedPasswordHistoryItem> items = this.GetPrunedHistoryItems(this.PasswordHistory, rotationInstant, maximumPasswordHistory);
            items.Insert(0, pphi);

            this.ReplacePasswordHistory(items, expiryDate);
        }

        public void ReplacePasswordHistory(IList<ProtectedPasswordHistoryItem> items, DateTime? expiryDate)
        {
            if (expiryDate != null)
            {
                this.de.Properties["msDS-DateTime"].Value = expiryDate.Value;
            }

            if (items != null)
            {
                if (!this.PasswordHistory.OrderBy(t => t.Created).ThenBy(t => t.EncryptedData).SequenceEqual(items.OrderBy(t => t.Created).ThenBy(t => t.EncryptedData)))
                {
                    this.de.Properties["msDS-Settings"].Clear();
                    this.de.Properties["msDS-Settings"].AddRange(items.Select(t => JsonConvert.SerializeObject(t)).ToArray());
                    this.passwordHistoryEntries = null;
                }
            }
            else
            {
                this.de.Properties["msDS-Settings"].Clear();
                this.passwordHistoryEntries = null;
            }

            this.de.CommitChanges();
        }

        public void ClearPasswordHistory()
        {
            this.de.Properties["msDS-Settings"].Clear();
            this.de.CommitChanges();
        }

        internal List<ProtectedPasswordHistoryItem> GetPrunedHistoryItems(IEnumerable<ProtectedPasswordHistoryItem> items, DateTime rotationInstant, int maximumPasswordHistoryDays)
        {
            List<ProtectedPasswordHistoryItem> newItems = new List<ProtectedPasswordHistoryItem>();

            if (maximumPasswordHistoryDays <= 0)
            {
                return newItems;
            }

            foreach (var item in items.OrderByDescending(t => t.Created))
            {
                if (item.Retired == null)
                {
                    item.Retired = rotationInstant;
                }

                if (item.Retired.Value.AddDays(maximumPasswordHistoryDays) > DateTime.UtcNow)
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