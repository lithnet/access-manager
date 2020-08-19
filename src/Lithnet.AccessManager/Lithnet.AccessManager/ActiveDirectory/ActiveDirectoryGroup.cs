using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using Lithnet.AccessManager.Interop;
using NLog.LayoutRenderers.Wrappers;
using SearchScope = System.DirectoryServices.SearchScope;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectoryGroup : IGroup
    {

        private readonly DirectoryEntry de;

        internal static string[] PropertiesToGet = new string[] { "samAccountName", "distinguishedName", "tokenGroups", "displayName", "objectGuid", "objectSid", "samAccountName", "msDS-PrincipalName", "objectClass", "entryTTL" };

        public ActiveDirectoryGroup(DirectoryEntry directoryEntry)
        {
            directoryEntry.ThrowIfNotObjectClass("group");
            this.de = directoryEntry;
            this.de.RefreshCache(PropertiesToGet);
        }

        public Guid? Guid => this.de.GetPropertyGuid("objectGuid");

        public string MsDsPrincipalName => this.de.GetPropertyString("msDS-PrincipalName");

        public SecurityIdentifier Sid => this.de.GetPropertySid("objectSid");

        public string SamAccountName => this.de.GetPropertyString("samAccountName");

        public string DistinguishedName => this.de.GetPropertyString("distinguishedName");

        public TimeSpan? EntryTtl => this.de.Properties.Contains("entryTTL") ? TimeSpan.FromSeconds(this.de.GetPropertyInteger("entryTTL") ?? 0) : (TimeSpan?)null;

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

        public void RemoveMember(ISecurityPrincipal principal)
        {
            de.Properties["member"].Remove($"<SID={principal.Sid}>");
            de.CommitChanges();
        }

        public void RemoveMembers()
        {
            de.Properties["member"].Clear();
            de.CommitChanges();
        }

        public IEnumerable<string> GetMemberTtlDNs()
        {
            LdapDirectoryIdentifier directory = new LdapDirectoryIdentifier(NativeMethods.GetDnsDomainNameFromSid(this.Sid));

            var connection = new LdapConnection(directory);

            List<string> attributesToGet = new List<string>() { "member" };

            SearchRequest r = new SearchRequest(
                this.DistinguishedName,
                "(objectClass=*)",
                System.DirectoryServices.Protocols.SearchScope.Base,
                attributesToGet.ToArray()
            );

            r.Controls.Add(new DirectoryControl("1.2.840.113556.1.4.2309", null, true, true));

            SearchResponse response = connection.SendRequest(r) as SearchResponse;

            if (response?.ResultCode != ResultCode.Success)
            {
                throw new DirectoryException($"The LDAP operation failed with result code {response?.ResultCode}");
            }

            foreach (SearchResultEntry entry in response.Entries)
            {
                if (!entry.Attributes.Contains("member"))
                {
                    continue;
                }

                foreach (var s in entry.Attributes["member"].GetValues(typeof(string)).OfType<string>())
                {
                    yield return s;
                }
            }
        }

        public TimeSpan? GetMemberTtl(IUser user)
        {
            var ttlMembers = this.GetMemberTtlDNs();

            foreach (var ttlmember in ttlMembers)
            {
                Match match = Regex.Match(ttlmember, "<TTL=(?<ttl>\\d+)>,(?<dn>.+)");

                if (!match.Success)
                {
                    continue;
                }

                string dn = match.Groups["dn"].Value;
                string ttl = match.Groups["ttl"].Value;
                string sidDn = $"CN={user.Sid},";

                if (string.Equals(user.DistinguishedName, dn, StringComparison.CurrentCultureIgnoreCase) ||
                    dn.StartsWith(sidDn, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(ttl, out int result))
                    {
                        return TimeSpan.FromSeconds(result);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return null;
        }


        public void ExtendTtl(TimeSpan ttl)
        {
            if (!this.de.GetPropertyStrings("objectClass").Contains("dynamicObject", StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The group is not a dynamic object");
            }

            de.Properties["entryTTL"].Value = (int)ttl.TotalSeconds;
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
