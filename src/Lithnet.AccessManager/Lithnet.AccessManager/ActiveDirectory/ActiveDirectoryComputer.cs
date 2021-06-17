using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryComputer : IActiveDirectoryComputer
    {
        internal static string[] PropertiesToGet = new string[] { "samAccountName", "distinguishedName", "description", "displayName", "objectGuid", "objectSid", "msDS-PrincipalName", "objectClass", "dNSHostName" };

        private readonly DirectoryEntry de;

        public ActiveDirectoryComputer(DirectoryEntry directoryEntry)
        {
            directoryEntry.ThrowIfNotObjectClass("computer");
            this.de = directoryEntry;
            this.de.RefreshCache(PropertiesToGet);
        }

        public string Path => this.de.Path;

        public string SamAccountName => this.de.GetPropertyString("samAccountName");

        public string DistinguishedName => this.de.GetPropertyString("distinguishedName");

        public string MsDsPrincipalName => this.de.GetPropertyString("msDS-PrincipalName");

        public string Description => this.de.GetPropertyString("description");

        public string DisplayName => this.de.GetPropertyString("displayName");

        public string DnsHostName => this.de.GetPropertyString("dNSHostName");

        public string Name => this.de.GetPropertyString("samAccountName");

        public string FullyQualifiedName => this.de.GetPropertyString("msDS-PrincipalName");

        public string ObjectID => this.de.GetPropertyGuid("objectGuid").ToString();

        public string Authority => this.Sid.AccountDomainSid.ToString();

        public AuthorityType AuthorityType => AuthorityType.ActiveDirectory;

        public string AuthorityDeviceId => this.Sid.ToString();

        public SecurityIdentifier SecurityIdentifier => this.Sid;

        public Guid? Guid => this.de.GetPropertyGuid("objectGuid");

        public SecurityIdentifier Sid => this.de.GetPropertySid("objectSid");

        public IEnumerable<Guid> GetParentGuids()
        {
            DirectoryEntry parent = this.de.Parent;

            while (!string.Equals(parent.SchemaClassName, "domainDNS", StringComparison.OrdinalIgnoreCase))
            {
                yield return parent.Guid;
                parent = parent.Parent;
            }

            yield return parent.Guid;
        }

        public DirectoryEntry DirectoryEntry => this.de;

        public string Type => "Computer";
    }
}