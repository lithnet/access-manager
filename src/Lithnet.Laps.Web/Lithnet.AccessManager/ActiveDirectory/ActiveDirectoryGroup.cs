using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryGroup : IGroup
    {

        private readonly DirectoryEntry de;

        internal static string[] PropertiesToGet = new string[] { "samAccountName", "distinguishedName", "tokenGroups", "displayName", "objectGuid", "objectSid", "samAccountName", "msDS-PrincipalName", "objectClass" };

        public ActiveDirectoryGroup(DirectoryEntry directoryEntry)
        {
            this.de = directoryEntry;
            this.de.RefreshCache(PropertiesToGet);
        }

        public Guid? Guid => this.de.GetPropertyGuid("objectGuid");

        public string MsDsPrincipalName =>  this.de.GetPropertyString("msDS-PrincipalName");

        public SecurityIdentifier Sid =>  this.de.GetPropertySid("objectSid");

        public string SamAccountName =>  this.de.GetPropertyString("samAccountName");

        public string DistinguishedName =>  this.de.GetPropertyString("distinguishedName");

        public void AddMember(ISecurityPrincipal principal, TimeSpan ttl)
        {
            de.Properties["member"].Add($"<TTL={ttl.TotalSeconds},<SID={principal.Sid}>>");
            de.CommitChanges();
        }

        public void AddMember(ISecurityPrincipal principal)
        {
            de.Properties["member"].Add($"<SID={principal.Sid}>");
            de.CommitChanges();
        }
        public IEnumerable<string> GetMemberDNs()
        {
            HashSet<string> memberDNs = new HashSet<string>();

            int rangeLower = 0;
            int rangeUpper = 1499;
            int rangeStep = 1500;

            while (true)
            {

                DirectorySearcher d = new DirectorySearcher
                {
                    SearchRoot = this.de,
                    SearchScope = SearchScope.Base,
                    Filter = "(&(objectClass=*))"
                };

                string attribute = $"member;range={rangeLower}-{rangeUpper}";

                d.PropertiesToLoad.Add(attribute);

                var result = d.FindOne();

                if (de == null)
                {
                    return memberDNs;
                }

                var returnedMemberPropertyName = result.Properties.PropertyNames.OfType<string>().FirstOrDefault(t => t.StartsWith("member;range=", StringComparison.OrdinalIgnoreCase));

                if (returnedMemberPropertyName == null)
                {
                    return memberDNs;
                }

                foreach (var item in result.Properties[returnedMemberPropertyName].OfType<string>())
                {
                    memberDNs.Add(item);
                }

                if (returnedMemberPropertyName.EndsWith("*"))
                {
                    return memberDNs;
                }

                rangeLower = rangeUpper + 1;
                rangeUpper += rangeStep;
            }
        }
    }
}
