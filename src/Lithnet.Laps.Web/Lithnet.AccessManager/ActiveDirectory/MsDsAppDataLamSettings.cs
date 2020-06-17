using System;
using System.Collections.Generic;
using System.DirectoryServices;
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

        private IReadOnlyList<PasswordHistoryEntry> passwordHistoryEntries;

        private readonly SearchResult settings;

        public MsDsAppDataLamSettings(SearchResult settings)
        {
            this.settings = settings;
        }

        public DateTime? PasswordExpiry => this.settings?.GetPropertyDateTimeFromLong("msDS-DateTime");

        public string DistinguishedName => this.settings?.GetPropertyString("distinguishedName");

        public string JitGroupReference => this.settings?.GetPropertyString("msDS-ObjectReference");

        public IReadOnlyList<PasswordHistoryEntry> PasswordHistory
        {
            get
            {
                if (this.passwordHistoryEntries == null)
                {
                    List<PasswordHistoryEntry> list = new List<PasswordHistoryEntry>();

                    foreach (string item in this.PasswordHistoryData)
                    {
                        list.Add(JsonConvert.DeserializeObject<PasswordHistoryEntry>(item));
                    }

                    this.passwordHistoryEntries = list.AsReadOnly();
                }

                return this.passwordHistoryEntries;
            }
        }

        public IEnumerable<string> PasswordHistoryData => this.settings?.GetPropertyStrings("msDS-Settings");

        public Guid? Guid => this.settings?.GetPropertyGuid("objectGuid");

        internal static DirectoryEntry Create(IComputer computer)
        {
            var parent = computer.GetDirectoryEntry();

            DirectoryEntry de = parent.Children.Add($"CN={AttrCommonName}", ObjectClass);
            de.Properties["applicationName"].Add(AttrApplicationName);
            de.Properties["description"].Add(AttrDescription);
            de.Properties["ntSecurityDescriptor"].Add(GetDefaultSecurityDescriptorForLamObject());

            return de;
        }

        private static byte[] GetDefaultSecurityDescriptorForLamObject()
        {
            ActiveDirectorySecurity gf = new ActiveDirectorySecurity();
            gf.SetSecurityDescriptorSddlForm("D:AI(A;;FA;;;CO)");
            return gf.GetSecurityDescriptorBinaryForm();
        }
    }
}