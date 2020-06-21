using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Lithnet.AccessManager.Interop;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectory : IDirectory
    {
        private readonly ILogger<ActiveDirectory> logger;

        private static Guid PamFeatureGuid = new Guid("ec43e873-cce8-4640-b4ab-07ffe4ab5bcd");

        private Dictionary<SecurityIdentifier, bool> PamEnabledDomainCache = new Dictionary<SecurityIdentifier, bool>();

        public ActiveDirectory(ILogger<ActiveDirectory> logger)
        {
            this.logger = logger;
        }

        public bool TryGetUser(string name, out IUser user)
        {
            return DirectoryExtensions.TryGet(() => this.GetUser(name), out user);
        }

        public IUser GetUser(string name)
        {
            return new ActiveDirectoryUser(this.DoGcLookup(name, "user", ActiveDirectoryUser.PropertiesToGet));
        }

        public IComputer GetComputer(string name)
        {
            return new ActiveDirectoryComputer(this.DoGcLookup(name, "computer", ActiveDirectoryComputer.PropertiesToGet));
        }

        public bool TryGetComputer(string name, out IComputer computer)
        {
            return DirectoryExtensions.TryGet(() => this.GetComputer(name), out computer);
        }

        public ISecurityPrincipal GetPrincipal(string name)
        {
            SearchResult result = this.DoGcLookup(name, "*", ActiveDirectoryComputer.PropertiesToGet);

            if (result.HasPropertyValue("objectClass", "computer"))
            {
                return new ActiveDirectoryComputer(result);
            }

            if (result.HasPropertyValue("objectClass", "group"))
            {
                return new ActiveDirectoryGroup(result);
            }

            if (result.HasPropertyValue("objectClass", "user"))
            {
                return new ActiveDirectoryUser(result);
            }

            throw new UnsupportedPrincipalTypeException($"The object '{name}' was of an unknown type: {result.GetPropertyCommaSeparatedString("objectClass")}");
        }

        public bool TryGetPrincipal(string name, out ISecurityPrincipal principal)
        {
            return DirectoryExtensions.TryGet(() => this.GetPrincipal(name), out principal);
        }

        public bool IsContainer(string path)
        {
            try
            {
                SearchResult result = this.GetDirectoryEntry(path, "*", "objectClass");

                return result.HasPropertyValue("objectClass", "organizationalUnit") ||
                       result.HasPropertyValue("objectClass", "domain") ||
                       result.HasPropertyValue("objectClass", "domainDNS") ||
                       result.HasPropertyValue("objectClass", "container");
            }
            catch
            {
                return false;
            }
        }

        public bool IsObjectInOu(IDirectoryObject o, string ou)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"GC://{ou}"),
                SearchScope = SearchScope.Subtree,
                Filter = $"objectGuid={o.Guid.ToOctetString()}"
            };

            return d.FindOne() != null;
        }

        public IGroup GetGroup(string groupName)
        {
            return new ActiveDirectoryGroup(this.DoGcLookup(groupName, "group", ActiveDirectoryGroup.PropertiesToGet));
        }

        public bool TryGetGroup(string name, out IGroup group)
        {
            return DirectoryExtensions.TryGet(() => this.GetGroup(name), out group);
        }

        public IGroup GetGroup(SecurityIdentifier sid)
        {
            return new ActiveDirectoryGroup(this.DoGcLookup(sid.ToString(), "group", ActiveDirectoryGroup.PropertiesToGet));
        }

        public bool TryGetGroup(SecurityIdentifier sid, out IGroup group)
        {
            return DirectoryExtensions.TryGet(() => this.GetGroup(sid), out group);
        }

        public IGroup CreateGroup(string name, string description, int groupType, DirectoryEntry ou)
        {
            string samAccountName = name;
            if (name.Contains('\\'))
            {
                samAccountName = name.Split('\\')[1];
            }

            DirectoryEntry de = ou.Children.Add($"CN={samAccountName}", "group");
            de.Properties["samAccountName"].Add(samAccountName);
            de.Properties["description"].Add(description);
            de.Properties["groupType"].Add(groupType);
            de.CommitChanges();

            return this.GetGroup(de.GetPropertySid("objectSid"));
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal)
        {
            return this.IsSidInPrincipalToken(sidToFindInToken, principal, principal.Sid.AccountDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid)
        {
            return NativeMethods.CheckForSidInToken(principal.Sid, sidToFindInToken, targetDomainSid);
        }

        public IEnumerable<string> GetMemberDNsFromGroup(IGroup group)
        {
            return this.GetMemberDNsFromGroup(group.DistinguishedName);
        }

        private IEnumerable<string> GetMemberDNsFromGroup(string dn)
        {
            HashSet<string> memberDNs = new HashSet<string>();

            int rangeLower = 0;
            int rangeUpper = 1499;
            int rangeStep = 1500;

            while (true)
            {
                var de = this.GetDirectoryEntry(dn, "group", $"member;range={rangeLower}-{rangeUpper}");

                if (de == null)
                {
                    return memberDNs;
                }

                var returnedMemberPropertyName = de.Properties.PropertyNames.OfType<string>().FirstOrDefault(t => t.StartsWith("member;range=", StringComparison.OrdinalIgnoreCase));

                if (returnedMemberPropertyName == null)
                {
                    return memberDNs;
                }

                foreach (var item in de.Properties[returnedMemberPropertyName].OfType<string>())
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

        public void AddGroupMember(IGroup group, ISecurityPrincipal principal, TimeSpan ttl)
        {
            var groupEntry = group.GetDirectoryEntry();
            groupEntry.Properties["member"].Add($"<TTL={ttl.TotalSeconds},<SID={principal.Sid}>>");
            groupEntry.CommitChanges();
        }

        public void AddGroupMember(IGroup group, ISecurityPrincipal principal)
        {
            var groupEntry = group.GetDirectoryEntry();
            groupEntry.Properties["member"].Add($"<SID={principal.Sid}>");
            groupEntry.CommitChanges();
        }

        public void CreateTtlGroup(string accountName, string displayName, string description, string ou, TimeSpan ttl)
        {
            DirectoryEntry container = new DirectoryEntry($"LDAP://{ou}");
            dynamic[] objectClasses = new dynamic[] { "dynamicObject", "group" };

            DirectoryEntry group = container.Children.Add($"CN={accountName}", "group");

            group.Invoke("Put", "objectClass", objectClasses);
            group.Properties["samAccountName"].Add(accountName);
            group.Properties["displayName"].Add(displayName);
            group.Properties["description"].Add(description);
            group.Properties["groupType"].Add(-2147483644);
            group.Properties["entryTTL"].Add((int)ttl.TotalSeconds);
            group.CommitChanges();
        }

        public bool IsPamFeatureEnabled(SecurityIdentifier domainSid)
        {
            SecurityIdentifier sid = domainSid.AccountDomainSid;

            if (PamEnabledDomainCache.TryGetValue(sid, out bool value))
            {
                return value;
            }

            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = this.GetConfigurationNamingContext(sid),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass=msDS-OptionalFeature)(msDS-OptionalFeatureGUID={PamFeatureGuid.ToOctetString()}))",
            };

            bool result = d.FindOne() != null;

            PamEnabledDomainCache.Add(domainSid, result);

            return result;
        }

        private DirectoryEntry GetConfigurationNamingContext(SecurityIdentifier domain)
        {
            SecurityIdentifier sid = domain.AccountDomainSid;

            string dc = NativeMethods.GetDnsDomainNameFromSid(sid);

            var rootDse = new DirectoryEntry($"LDAP://{dc}/rootDSE");

            var configNamingContext = (string)rootDse.Properties["configurationNamingContext"]?.Value;

            if (configNamingContext == null)
            {
                throw new ObjectNotFoundException($"Configuration naming context lookup failed");
            }

            return new DirectoryEntry($"LDAP://{configNamingContext}");
        }

        private SearchResult DoGcLookup(string objectName, string objectClass, IEnumerable<string> propertiesToGet)
        {
            string dn;

            if (objectClass.Equals("computer", StringComparison.OrdinalIgnoreCase) && !objectName.EndsWith("$"))
            {
                objectName += "$";
            }

            if (objectName.Contains("\\") || objectName.Contains("@"))
            {
                dn = NativeMethods.GetDn(objectName);
            }
            else if (objectName.TryParseAsSid(out SecurityIdentifier sid))
            {
                dn = $"<SID={sid}>";
            }
            else if (this.IsDistinguishedName(objectName))
            {
                dn = objectName;
            }
            else
            {
                dn = ActiveDirectory.DoGcLookupFromSimpleName(objectName, objectClass);
            }

            if (dn == null)
            {
                throw new ObjectNotFoundException($"An object {objectName} of type {objectClass} was not found in the global catalog");
            }

            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"LDAP://{dn}"),
                SearchScope = SearchScope.Base,
                Filter = $"(objectClass={objectClass})"
            };

            foreach (string prop in propertiesToGet)
            {
                d.PropertiesToLoad.Add(prop);
            }

            d.PropertiesToLoad.AddIfMissing("objectClass", StringComparer.OrdinalIgnoreCase);

            SearchResult result = d.FindOne();

            if (result == null)
            {
                throw new ObjectNotFoundException($"The object {dn} was not found in the directory or was not of the object class {objectClass}");
            }

            return result;
        }

        private static string DoGcLookupFromSimpleName(string samAccountName, string objectClass)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"GC://{Forest.GetCurrentForest().Name}"),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass={objectClass})(samAccountName={ActiveDirectory.EscapeSearchFilterParameter(samAccountName)}))"
            };

            d.PropertiesToLoad.Add("distinguishedName");

            SearchResultCollection result = d.FindAll();

            if (result.Count > 1)
            {
                throw new AmbiguousNameException($"There was more than one value in the directory that matched the name {samAccountName}");
            }

            if (result.Count == 0)
            {
                return null;
            }

            return result[0].Properties["distinguishedName"][0].ToString();
        }

        public SearchResult GetDirectoryEntry(ISecurityPrincipal principal, params string[] propertiesToLoad)
        {
            return this.GetDirectoryEntry(principal.DistinguishedName, "*", propertiesToLoad);
        }

        public SearchResult SearchDirectoryEntry(string basedn, string filter, SearchScope scope, params string[] propertiesToLoad)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"LDAP://{basedn}"),
                SearchScope = scope,
                Filter = filter
            };

            foreach (string prop in propertiesToLoad)
            {
                d.PropertiesToLoad.Add(prop);
            }

            return d.FindOne();
        }

        public SearchResult GetDirectoryEntry(string dn, string objectClass, params string[] propertiesToLoad)
        {
            return this.SearchDirectoryEntry(dn, $"objectClass={objectClass}", SearchScope.Base, propertiesToLoad);
        }

        private static string EscapeSearchFilterParameter(string p)
        {
            StringBuilder escapedValue = new StringBuilder();

            foreach (char c in p)
            {
                switch (c)
                {
                    case '\\':
                        escapedValue.Append("\\5c");
                        break;

                    case '*':
                        escapedValue.Append("\\2a");
                        break;

                    case '(':
                        escapedValue.Append("\\28");
                        break;

                    case ')':
                        escapedValue.Append("\\29");
                        break;

                    case '\0':
                        escapedValue.Append("\\00");
                        break;

                    default:
                        escapedValue.Append(c);
                        break;
                }
            }

            return escapedValue.ToString();
        }

        private bool IsDistinguishedName(string name)
        {
            if (!name.Contains('='))
            {
                return false;
            }

            try
            {
                X500DistinguishedName d2 = new X500DistinguishedName(name);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}