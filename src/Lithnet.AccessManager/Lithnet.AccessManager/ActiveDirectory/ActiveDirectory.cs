using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectory : IDirectory
    {
        private static readonly Guid PamFeatureGuid = new Guid("ec43e873-cce8-4640-b4ab-07ffe4ab5bcd");


        private readonly Dictionary<string, bool> pamEnabledDomainCache = new Dictionary<string, bool>();

        public bool TryGetUser(string name, out IUser user)
        {
            return DirectoryExtensions.TryGet(() => this.GetUser(name), out user);
        }

        public IUser GetUser(string name)
        {
            return new ActiveDirectoryUser(this.DoGcLookup(name, "user", ActiveDirectoryUser.PropertiesToGet));
        }

        public IUser GetUser(SecurityIdentifier sid)
        {
            return new ActiveDirectoryUser(this.DoGcLookup(sid.ToString(), "user", ActiveDirectoryUser.PropertiesToGet));
        }

        public IComputer GetComputer(string name)
        {
            return new ActiveDirectoryComputer(this.DoGcLookup(name, "computer", ActiveDirectoryComputer.PropertiesToGet));
        }

        public IComputer GetComputer(SecurityIdentifier sid)
        {
            return new ActiveDirectoryComputer(this.DoGcLookup(sid.ToString(), "computer", ActiveDirectoryComputer.PropertiesToGet));
        }

        public bool TryGetComputer(string name, out IComputer computer)
        {
            return DirectoryExtensions.TryGet(() => this.GetComputer(name), out computer);
        }

        public ISecurityPrincipal GetPrincipal(SecurityIdentifier sid)
        {
            var result = NativeMethods.GetDirectoryEntry(sid);

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

            throw new UnsupportedPrincipalTypeException($"The object '{sid}' was of an unknown type: {result.GetPropertyCommaSeparatedString("objectClass")}");
        }

        public ISecurityPrincipal GetPrincipal(string name)
        {
            var result = this.DoGcLookup(name, "*", ActiveDirectoryComputer.PropertiesToGet);

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
        public bool TryGetPrincipal(SecurityIdentifier sid, out ISecurityPrincipal principal)
        {
            return DirectoryExtensions.TryGet(() => this.GetPrincipal(sid), out principal);
        }

        public bool IsContainer(DirectoryEntry de)
        {
            try
            {
                return de.HasPropertyValue("objectClass", "organizationalUnit") ||
                       de.HasPropertyValue("objectClass", "domain") ||
                       de.HasPropertyValue("objectClass", "domainDNS") ||
                       de.HasPropertyValue("objectClass", "container");
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
                Filter = $"objectGuid={o.Guid.ToOctetString()}",
                PropertyNamesOnly = true
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

        public void CreateGroup(string name, string description, GroupType groupType, string ou)
        {
            DirectoryEntry oude = new DirectoryEntry($"LDAP://{ou}");
            string samAccountName = name;
            if (name.Contains('\\'))
            {
                samAccountName = name.Split('\\')[1];
            }

            DirectoryEntry de = oude.Children.Add($"CN={samAccountName}", "group");
            de.Properties["samAccountName"].Add(samAccountName);
            de.Properties["description"].Add(description);
            de.Properties["groupType"].Add(unchecked((int)groupType));
            de.CommitChanges();
        }

        public void DeleteGroup(string name)
        {
            var result = this.DoGcLookup(name, "group", null);
            result.DeleteTree();
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal)
        {
            return this.IsSidInPrincipalToken(sidToFindInToken, principal, principal.Sid.AccountDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid)
        {
            return NativeMethods.CheckForSidInToken(principal.Sid, sidToFindInToken, targetDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal)
        {
            return this.IsSidInPrincipalToken(sidToFindInToken, principal, principal.AccountDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal, SecurityIdentifier targetDomainSid)
        {
            return NativeMethods.CheckForSidInToken(principal, sidToFindInToken, targetDomainSid);
        }

        public IEnumerable<SecurityIdentifier> GetTokenGroups(ISecurityPrincipal principal)
        {
            return this.GetTokenGroups(principal, null);
        }

        public IEnumerable<SecurityIdentifier> GetTokenGroups(ISecurityPrincipal principal, SecurityIdentifier targetDomainSid)
        {
            return NativeMethods.GetTokenGroups(principal.Sid, targetDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFind, IEnumerable<SecurityIdentifier> tokenSids)
        {
            return tokenSids.Any(t => sidToFind == t);
        }

        public string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat)
        {
            return this.TranslateName(name, nameFormat, requiredFormat, null);
        }

        public string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat, string dnsDomainName)
        {
            return NativeMethods.CrackNames(nameFormat, requiredFormat, name, dnsDomainName).Name;
        }

        public IGroup CreateTtlGroup(string accountName, string displayName, string description, string ou, TimeSpan ttl)
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

            return new ActiveDirectoryGroup(group);
        }

        public bool IsPamFeatureEnabled(SecurityIdentifier domainSid)
        {
            SecurityIdentifier sid = domainSid.AccountDomainSid;

            return this.IsPamFeatureEnabled(NativeMethods.GetDnsDomainNameFromSid(sid));
        }

        public bool IsPamFeatureEnabled(string dnsDomain)
        {
            if (pamEnabledDomainCache.TryGetValue(dnsDomain, out bool value))
            {
                return value;
            }

            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = this.GetConfigurationNamingContext(dnsDomain),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass=msDS-OptionalFeature)(msDS-OptionalFeatureGUID={PamFeatureGuid.ToOctetString()}))",
            };

            bool result = d.FindOne() != null;

            pamEnabledDomainCache.Add(dnsDomain, result);

            return result;
        }

        internal string GetContextDn(string contextName, string dnsDomain)
        {
            var rootDse = new DirectoryEntry($"LDAP://{dnsDomain}/rootDSE");

            var context = (string)rootDse.Properties[contextName]?.Value;

            if (context == null)
            {
                throw new ObjectNotFoundException($"Naming context lookup failed for {contextName}");
            }

            return context;
        }

        public string GetDomainNameNetBiosFromSid(SecurityIdentifier sid)
        {
            return TranslateName(sid.AccountDomainSid.ToString(), DsNameFormat.SecurityIdentifier, DsNameFormat.Nt4Name).Trim('\\');
        }

        public string GetDomainNameDnsFromSid(SecurityIdentifier sid)
        {
            return NativeMethods.GetDnsDomainNameFromSid(sid);
        }

        public string GetDomainNameDnsFromDn(string dn)
        {
            var result = NativeMethods.CrackNames(DsNameFormat.DistinguishedName, DsNameFormat.DistinguishedName, dn);
            return result.Domain;
        }

        public string GetDomainControllerForDomain(string domainDns)
        {
            return this.GetDomainControllerForDomain(domainDns, false);
        }

        public string GetDomainControllerForDomain(string domainDns, bool forceRediscovery)
        {
            return NativeMethods.GetDomainControllerForDnsDomain(domainDns, forceRediscovery);
        }


        public DirectoryEntry GetConfigurationNamingContext(SecurityIdentifier domain)
        {
            SecurityIdentifier sid = domain.AccountDomainSid;

            string dc = NativeMethods.GetDnsDomainNameFromSid(sid);

            return new DirectoryEntry($"LDAP://{this.GetConfigurationNamingContext(dc)}");
        }

        public DirectoryEntry GetConfigurationNamingContext(string dnsDomain)
        {
            return new DirectoryEntry($"LDAP://{this.GetContextDn("configurationNamingContext", dnsDomain)}");
        }

        public DirectoryEntry GetSchemaNamingContext(SecurityIdentifier domain)
        {
            SecurityIdentifier sid = domain.AccountDomainSid;

            string dc = NativeMethods.GetDnsDomainNameFromSid(sid);

            return new DirectoryEntry($"LDAP://{this.GetSchemaNamingContext(dc)}");
        }

        public DirectoryEntry GetSchemaNamingContext(string dnsDomain)
        {
            return new DirectoryEntry($"LDAP://{this.GetContextDn("schemaNamingContext", dnsDomain)}");
        }

        public bool DoesSchemaAttributeExist(string dnsDomain, string attributeName)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = this.GetSchemaNamingContext(dnsDomain),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass=attributeSchema)(lDAPDisplayName={attributeName}))"
            };

            d.PropertiesToLoad.Add("distinguishedName");

            SearchResultCollection result = d.FindAll();

            if (result.Count > 1)
            {
                throw new InvalidOperationException($"More than one attribute called {attributeName} was found");
            }

            if (result.Count == 0)
            {
                return false;
            }

            return true;
        }

        private DirectoryEntry DoGcLookup(string objectName, string objectClass, IEnumerable<string> propertiesToGet)
        {
            if (objectName.TryParseAsSid(out SecurityIdentifier sid))
            {
                return NativeMethods.GetDirectoryEntry(sid);
            }

            if (objectClass.Equals("computer", StringComparison.OrdinalIgnoreCase) && !objectName.EndsWith("$"))
            {
                objectName += "$";
            }

            if (objectName.Contains("\\"))
            {
                return NativeMethods.GetDirectoryEntry(objectName, DsNameFormat.Nt4Name);
            }
            else if (objectName.Contains("@"))
            {
                return NativeMethods.GetDirectoryEntry(objectName, DsNameFormat.UserPrincipalName);
            }
            else if (this.IsDistinguishedName(objectName))
            {
                return NativeMethods.GetDirectoryEntry(objectName, DsNameFormat.DistinguishedName);
            }
            else
            {
                string dn = ActiveDirectory.GcGetDnFromSimpleName(objectName, objectClass);

                if (dn == null)
                {
                    throw new ObjectNotFoundException(
                        $"An object {objectName} of type {objectClass} was not found in the global catalog");
                }

                var de = new DirectoryEntry($"LDAP://{dn}");

                return de;
            }
        }

        private static string GcGetDnFromSimpleName(string samAccountName, string objectClass)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"GC://{Forest.GetCurrentForest().Name}"),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass={objectClass})(samAccountName={ActiveDirectory.EscapeSearchFilterParameter(samAccountName)}))"
            };

            d.PropertiesToLoad.Add("distinguishedName");

            using SearchResultCollection result = d.FindAll();

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