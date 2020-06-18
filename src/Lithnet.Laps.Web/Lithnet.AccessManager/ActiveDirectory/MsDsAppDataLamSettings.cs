using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using Newtonsoft.Json;

namespace Lithnet.AccessManager
{
    public sealed class MsDsAppDataLamSettings : ILamSettings
    {
        internal static string[] PropertiesToGet = new string[] { "applicationName", "description", "msDS-ObjectReference", "msDS-Settings", "objectGuid", "objectClass", "msDS-DateTime" };

        internal const string ObjectClass = "msDs-App-Configuration";

        private const string AttrApplicationName = "Lithnet Access Manager";

        private const string AttrDescription = "Application configuration for Lithnet Access Manager";

        private const string AttrCommonName = "LithnetAccessManagerConfig";

        internal static string Filter = $"(&(objectClass={ObjectClass})(applicationName={AttrApplicationName}))";

        private IReadOnlyList<ProtectedPasswordHistoryItem> passwordHistoryEntries;

        private readonly SearchResult sr;

        public MsDsAppDataLamSettings(SearchResult sr)
        {
            this.sr = sr;
        }

        public DateTime? PasswordExpiry => this.sr?.GetPropertyDateTimeFromLong("msDS-DateTime");

        public string DistinguishedName => this.sr?.GetPropertyString("distinguishedName");

        public string JitGroupReference => this.sr?.GetPropertyString("msDS-ObjectReference");

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

        public IEnumerable<string> PasswordHistoryData => this.sr?.GetPropertyStrings("msDS-Settings");

        public Guid? Guid => this.sr?.GetPropertyGuid("objectGuid");


        public void UpdateJitGroup(IGroup group)
        {
            DirectoryEntry de = this.sr.GetDirectoryEntry();

            if (group != null)
            {
                if (!DirectoryExtensions.IsDnMatch(this.JitGroupReference, group.DistinguishedName))
                {
                    de.Properties["msDS-ObjectReference"].Clear();
                    de.Properties["msDS-ObjectReference"].Add(group.DistinguishedName);
                    de.CommitChanges();
                }
            }
            else
            {
                de.Properties["msDS-ObjectReference"].Clear();
                de.CommitChanges();
            }

        }

        public void ReplacePasswordHistory(IList<ProtectedPasswordHistoryItem> items)
        {
            DirectoryEntry de = this.sr.GetDirectoryEntry();

            if (items != null)
            {
                if (!this.PasswordHistory.OrderBy(t => t.Created).ThenBy(t => t.EncryptedData).SequenceEqual(items.OrderBy(t => t.Created).ThenBy(t => t.EncryptedData)))
                {
                    de.Properties["msDS-Settings"].Clear();
                    de.Properties["msDS-Settings"].AddRange(items.Select(t => JsonConvert.SerializeObject(t)).ToArray());
                    de.CommitChanges();
                }
            }
            else
            {
                de.Properties["msDS-Settings"].Clear();
                de.CommitChanges();
            }
        }

        internal static ILamSettings Create(IComputer computer)
        {
            var parent = computer.GetDirectoryEntry();

            DirectoryEntry de = parent.Children.Add($"CN={AttrCommonName}", ObjectClass);
            de.Properties["applicationName"].Add(AttrApplicationName);
            de.Properties["description"].Add(AttrDescription);
            de.Properties["ntSecurityDescriptor"].Add(GetDefaultSecurityDescriptorForLamObject());
            de.CommitChanges();

            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = de,
                SearchScope = SearchScope.Base,
                Filter = "(objectClass=*)"
            };

            foreach (string property in MsDsAppDataLamSettings.PropertiesToGet)
            {
                d.PropertiesToLoad.Add(property);
            }

            var result = d.FindOne();

            if (result == null)
            {
                throw new ObjectNotFoundException();
            }

            return new MsDsAppDataLamSettings(result);
        }

        private static byte[] GetDefaultSecurityDescriptorForLamObject()
        {
            ActiveDirectorySecurity gf = new ActiveDirectorySecurity();
            gf.SetSecurityDescriptorSddlForm("D:AI(A;;FA;;;CO)");
            return gf.GetSecurityDescriptorBinaryForm();
        }
    }
}