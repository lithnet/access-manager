using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryLamSettings : ILamSettings
    {
        internal static string[] PropertiesToGet = new string[] { "applicationName", "description", "msDS-ObjectReference", "msDS-Settings", "objectGuid" };

        private readonly SearchResult settings;

        public ActiveDirectoryLamSettings(SearchResult settings)
        {
            this.settings = settings;
        }

        public string ApplicationName => this.settings?.GetPropertyString("applicationName");

        public string DistinguishedName => this.settings?.GetPropertyString("distinguishedName");

        public string Description => this.settings?.GetPropertyString("description");

        public string MsDsObjectReference => this.settings?.GetPropertyString("msDS-ObjectReference");

        public IEnumerable<string> MsDsSettings => this.settings?.GetPropertyStrings("msDS-Settings");

        public Guid? Guid => this.settings?.GetPropertyGuid("objectGuid");
    }
}