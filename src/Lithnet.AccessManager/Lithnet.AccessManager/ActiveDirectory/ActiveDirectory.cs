using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Lithnet.AccessManager.Interop;
using Lithnet.Security.Authorization;

namespace Lithnet.AccessManager
{
    public sealed class ActiveDirectory : IDirectory
    {
        private static readonly Guid PamFeatureGuid = new Guid("ec43e873-cce8-4640-b4ab-07ffe4ab5bcd");

        private static readonly Dictionary<string, bool> pamEnabledDomainCache = new Dictionary<string, bool>();

        private readonly IDiscoveryServices discoveryServices;

        private static SecurityIdentifier currentDomainSid;

        private static SecurityIdentifier CurrentDomainSid
        {
            get
            {
                if (currentDomainSid == null)
                {
                    Domain domain = Domain.GetComputerDomain();
                    currentDomainSid = new SecurityIdentifier((byte[])(domain.GetDirectoryEntry().Properties["objectSid"][0]), 0);
                }

                return currentDomainSid;
            }
        }

        public ActiveDirectory(IDiscoveryServices discoveryServices)
        {
            this.discoveryServices = discoveryServices;
        }

        public bool TryGetUser(string name, out IUser user)
        {
            return DirectoryExtensions.TryGet(() => this.GetUser(name), out user);
        }

        public IUser GetUser(string name)
        {
            return new ActiveDirectoryUser(this.FindUserInGc(name));
        }

        public bool TryGetUserByAltSecurityIdentity(string altSecurityIdentityValue, out IUser user)
        {
            user = null;
            string dn = this.GcGetDnFromAttributeSearch("altSecurityIdentities", altSecurityIdentityValue, "user");

            if (dn == null)
            {
                return false;
            }

            user = new ActiveDirectoryUser(this.GetDirectoryEntry(dn, DsNameFormat.DistinguishedName));

            return true;
        }

        public IUser GetUser(SecurityIdentifier sid)
        {
            return new ActiveDirectoryUser(this.FindUserInGc(sid.ToString()));
        }

        public IActiveDirectoryComputer GetComputer(string name)
        {
            return new ActiveDirectoryComputer(this.FindComputerInGc(name));
        }

        public IActiveDirectoryComputer GetComputer(SecurityIdentifier sid)
        {
            return new ActiveDirectoryComputer(this.FindComputerInGc(sid.ToString()));
        }

        public bool TryGetComputer(string name, out IActiveDirectoryComputer computer)
        {
            return DirectoryExtensions.TryGet(() => this.GetComputer(name), out computer);
        }

        public bool TryGetComputer(SecurityIdentifier sid, out IActiveDirectoryComputer computer)
        {
            return DirectoryExtensions.TryGet(() => this.GetComputer(sid), out computer);
        }


        public ISecurityPrincipal GetPrincipal(SecurityIdentifier sid)
        {
            var result = GetDirectoryEntry(sid);

            if (result.HasPropertyValue("objectClass", "msDS-GroupManagedServiceAccount"))
            {
                return new ActiveDirectoryGroupManagedServiceAccount(result);
            }

            if (result.HasPropertyValue("objectClass", "computer"))
            {
                return new ActiveDirectoryComputer(result);
            }

            if (result.HasPropertyValue("objectClass", "group"))
            {
                return new ActiveDirectoryGroup(result, this.discoveryServices);
            }

            if (result.HasPropertyValue("objectClass", "user"))
            {
                return new ActiveDirectoryUser(result);
            }

            throw new UnsupportedPrincipalTypeException($"The object '{sid}' was of an unknown type: {result.GetPropertyCommaSeparatedString("objectClass")}");
        }

        public ISecurityPrincipal GetPrincipal(string name)
        {
            var result = this.FindInGc(name, "*");

            if (result.HasPropertyValue("objectClass", "msDS-GroupManagedServiceAccount"))
            {
                return new ActiveDirectoryGroupManagedServiceAccount(result);
            }

            if (result.HasPropertyValue("objectClass", "computer"))
            {
                return new ActiveDirectoryComputer(result);
            }

            if (result.HasPropertyValue("objectClass", "group"))
            {
                return new ActiveDirectoryGroup(result, this.discoveryServices);
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
            return new ActiveDirectoryGroup(this.FindGroupInGc(groupName), this.discoveryServices);
        }

        public bool TryGetGroup(string name, out IGroup group)
        {
            return DirectoryExtensions.TryGet(() => this.GetGroup(name), out group);
        }

        public IGroup GetGroup(SecurityIdentifier sid)
        {
            return new ActiveDirectoryGroup(this.FindGroupInGc(sid.ToString()), this.discoveryServices);
        }

        public bool TryGetGroup(SecurityIdentifier sid, out IGroup group)
        {
            return DirectoryExtensions.TryGet(() => this.GetGroup(sid), out group);
        }

        public void CreateGroup(string name, string description, GroupType groupType, string ou, bool removeAccountOperators)
        {
            this.discoveryServices.FindDcAndExecuteWithRetry(this.discoveryServices.GetDomainNameDns(ou), dc =>
            {
                DirectoryEntry oude = new DirectoryEntry($"LDAP://{dc}/{ou}");
                string samAccountName = name;
                if (name.Contains('\\'))
                {
                    samAccountName = name.Split('\\')[1];
                }

                if (groupType == 0)
                {
                    groupType = GroupType.DomainLocal;
                }

                DirectoryEntry de = oude.Children.Add($"CN={samAccountName}", "group");
                de.Properties["samAccountName"].Add(samAccountName);
                de.Properties["description"].Add(description);
                de.Properties["groupType"].Add(unchecked((int)groupType));

                de.CommitChanges();

                if (removeAccountOperators)
                {
                    de.ObjectSecurity.RemoveAccess(new SecurityIdentifier(WellKnownSidType.BuiltinAccountOperatorsSid, null), AccessControlType.Allow);
                    de.CommitChanges();
                }
            });
        }

        public void DeleteGroup(string name)
        {
            var result = this.FindGroupInGc(name);
            result.DeleteTree();
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal)
        {
            return this.IsSidInPrincipalToken(sidToFindInToken, principal, principal.Sid.AccountDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid)
        {
            return CheckForSidInToken(principal.Sid, sidToFindInToken, targetDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal)
        {
            return this.IsSidInPrincipalToken(sidToFindInToken, principal, principal.AccountDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal, SecurityIdentifier targetDomainSid)
        {
            return CheckForSidInToken(principal, sidToFindInToken, targetDomainSid);
        }

        public IEnumerable<SecurityIdentifier> GetTokenGroups(ISecurityPrincipal principal)
        {
            return this.GetTokenGroups(principal, null);
        }

        public IEnumerable<SecurityIdentifier> GetTokenGroups(ISecurityPrincipal principal, SecurityIdentifier targetDomainSid)
        {
            return GetTokenGroups(principal.Sid, targetDomainSid);
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
            return this.discoveryServices.FindDcAndExecuteWithRetry(dnsDomainName, dc => NativeMethods.CrackNames(nameFormat, requiredFormat, name, dc, dnsDomainName).Name);
        }

        public IGroup CreateTtlGroup(string accountName, string displayName, string description, string ou, string targetDc, TimeSpan ttl, GroupType groupType, bool removeAccountOperators)
        {
            DirectoryEntry container = new DirectoryEntry($"LDAP://{targetDc}/{ou}");
            dynamic[] objectClasses = new dynamic[] { "dynamicObject", "group" };

            DirectoryEntry group = container.Children.Add($"CN={accountName}", "group");
            if (groupType == 0)
            {
                groupType = GroupType.DomainLocal;
            }

            group.Invoke("Put", "objectClass", objectClasses);
            group.Properties["samAccountName"].Add(accountName);
            group.Properties["displayName"].Add(displayName);
            group.Properties["description"].Add(description);
            group.Properties["groupType"].Add(unchecked((int)groupType));
            group.Properties["entryTTL"].Add((int)ttl.TotalSeconds);
            group.CommitChanges();

            if (removeAccountOperators)
            {
                group.ObjectSecurity.RemoveAccess(new SecurityIdentifier(WellKnownSidType.BuiltinAccountOperatorsSid, null), AccessControlType.Allow);
                group.CommitChanges();
            }

            return new ActiveDirectoryGroup(group, this.discoveryServices);
        }

        public bool IsPamFeatureEnabled(SecurityIdentifier domainSid, bool forceRefresh)
        {
            SecurityIdentifier sid = domainSid.AccountDomainSid;

            return this.IsPamFeatureEnabled(this.discoveryServices.GetDomainNameDns(sid), forceRefresh);
        }

        public bool IsPamFeatureEnabled(string dnsDomain, bool forceRefresh)
        {
            if (pamEnabledDomainCache.TryGetValue(dnsDomain, out bool value))
            {
                if (!forceRefresh)
                {
                    return value;
                }
                else
                {
                    pamEnabledDomainCache.Remove(dnsDomain);
                }
            }

            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = this.discoveryServices.GetConfigurationNamingContext(dnsDomain),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass=msDS-OptionalFeature)(msDS-OptionalFeatureGUID={PamFeatureGuid.ToOctetString()}))",
            };

            d.PropertiesToLoad.Add("msDS-EnabledFeatureBL");

            var item = d.FindOne();
            bool result = false;

            if (item != null)
            {
                result = item.GetPropertyStrings("msDS-EnabledFeatureBL")?.Any() ?? false;
            }

            pamEnabledDomainCache.Add(dnsDomain, result);

            return result;
        }

        public bool CanAccountBeDelegated(SecurityIdentifier serviceAccount)
        {
            using (PrincipalContext ctx = new PrincipalContext(ContextType.Domain))
            {
                using (UserPrincipal u = UserPrincipal.FindByIdentity(ctx, IdentityType.Sid, serviceAccount.ToString()))
                {
                    if (u != null)
                    {
                        return u.DelegationPermitted;
                    }
                }

                using (ComputerPrincipal cmp = ComputerPrincipal.FindByIdentity(ctx, IdentityType.Sid, serviceAccount.ToString()))
                {

                    if (cmp != null)
                    {
                        return cmp.DelegationPermitted;
                    }
                }
            }

            return false;
        }

        public bool IsAccountGmsa(SecurityIdentifier serviceAccount)
        {
            using (PrincipalContext ctx = new PrincipalContext(ContextType.Domain))
            {
                using (ComputerPrincipal cmp = ComputerPrincipal.FindByIdentity(ctx, IdentityType.Sid, serviceAccount.ToString()))
                {

                    if (cmp != null)
                    {
                        return string.Equals(cmp.StructuralObjectClass, "msDS-GroupManagedServiceAccount", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            return false;
        }

        private DirectoryEntry FindComputerInGc(string objectName)
        {
            DirectoryEntry de;

            if (objectName.TryParseAsSid(out SecurityIdentifier sid))
            {
                de = GetDirectoryEntry(sid);
            }
            else if (objectName.Contains("."))
            {
                string dn = GcGetDnFromAttributeSearch("dnsHostName", objectName, "computer");

                if (dn == null)
                {
                    var parts = objectName.Split('.');

                    if (parts.Length < 2)
                    {
                        throw new ObjectNotFoundException($"An object {objectName} of type computer was not found in the global catalog");
                    }

                    string hostname = parts[0];
                    string domain = string.Join(".", parts.Skip(1).ToArray());
                    string netbiosDomain;
                    try
                    {
                        netbiosDomain = this.discoveryServices.GetDomainNameNetBios(domain);
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 1355)
                    {
                        throw new ObjectNotFoundException($"Could not find computer {objectName} because the domain name {domain} could not be translated to a netBIOS name", ex);
                    }

                    string nt4Name = $"{netbiosDomain}\\{hostname}$";

                    return GetDirectoryEntry(nt4Name, DsNameFormat.Nt4Name);
                }

                de = GetDirectoryEntry(dn, DsNameFormat.DistinguishedName);
            }
            else if (this.IsDistinguishedName(objectName))
            {
                de = GetDirectoryEntry(objectName, DsNameFormat.DistinguishedName);
            }
            else
            {
                if (!objectName.EndsWith("$"))
                {
                    objectName += "$";
                }

                if (objectName.Contains("\\"))
                {
                    return GetDirectoryEntry(objectName, DsNameFormat.Nt4Name);
                }
                else
                {
                    string dn = GcGetDnFromAttributeSearch("samAccountName", objectName, "computer") ??
                                GcGetDnFromAttributeSearch("cn", objectName.TrimEnd('$'), "computer");

                    if (dn == null)
                    {
                        throw new ObjectNotFoundException(
                            $"An object {objectName} of type computer was not found in the global catalog");
                    }

                    de = GetDirectoryEntry(dn, DsNameFormat.DistinguishedName);
                }
            }

            if (de == null)
            {
                throw new ObjectNotFoundException(
                    $"An object {objectName} of type computer was not found in the global catalog");
            }

            de.ThrowIfNotObjectClass("computer");

            return de;
        }

        private DirectoryEntry FindUserInGc(string objectName)
        {
            DirectoryEntry de;

            if (objectName.TryParseAsSid(out SecurityIdentifier sid))
            {
                de = GetDirectoryEntry(sid);
            }

            else if (objectName.Contains("\\"))
            {
                de = GetDirectoryEntry(objectName, DsNameFormat.Nt4Name);
            }
            else if (objectName.Contains("@"))
            {
                de = GetDirectoryEntry(objectName, DsNameFormat.UserPrincipalName);
            }
            else if (this.IsDistinguishedName(objectName))
            {
                de = GetDirectoryEntry(objectName, DsNameFormat.DistinguishedName);
            }
            else
            {
                string dn = GcGetDnFromAttributeSearch("samAccountName", objectName, "user");

                if (dn == null)
                {
                    throw new ObjectNotFoundException(
                        $"An object {objectName} of type user was not found in the global catalog");
                }

                de = GetDirectoryEntry(dn, DsNameFormat.DistinguishedName);
            }

            if (de == null)
            {
                throw new ObjectNotFoundException(
                    $"An object {objectName} of type user was not found in the global catalog");
            }

            de.ThrowIfNotObjectClass("user");

            return de;
        }

        private DirectoryEntry FindGroupInGc(string objectName)
        {
            DirectoryEntry de;

            if (objectName == null)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            if (objectName.TryParseAsSid(out SecurityIdentifier sid))
            {
                de = GetDirectoryEntry(sid);
            }

            else if (objectName.Contains("\\"))
            {
                de = GetDirectoryEntry(objectName, DsNameFormat.Nt4Name);
            }
            else if (this.IsDistinguishedName(objectName))
            {
                de = GetDirectoryEntry(objectName, DsNameFormat.DistinguishedName);
            }
            else
            {
                string dn = GcGetDnFromAttributeSearch("samAccountName", objectName, "group");

                if (dn == null)
                {
                    throw new ObjectNotFoundException($"An object {objectName} of type group was not found in the global catalog");
                }

                de = GetDirectoryEntry(dn, DsNameFormat.DistinguishedName);
            }

            if (de == null)
            {
                throw new ObjectNotFoundException($"An object {objectName} of type group was not found in the global catalog");
            }

            de.ThrowIfNotObjectClass("group");

            return de;
        }

        private DirectoryEntry FindInGc(string objectName, string objectClass)
        {
            if (objectName.TryParseAsSid(out SecurityIdentifier sid))
            {
                return GetDirectoryEntry(sid);
            }

            if (objectClass.Equals("computer", StringComparison.OrdinalIgnoreCase) && !objectName.Contains(".") && !objectName.EndsWith("$"))
            {
                objectName += "$";
            }

            if (objectName.Contains("\\"))
            {
                return GetDirectoryEntry(objectName, DsNameFormat.Nt4Name);
            }
            else if (objectName.Contains("@"))
            {
                return GetDirectoryEntry(objectName, DsNameFormat.UserPrincipalName);
            }
            else if (this.IsDistinguishedName(objectName))
            {
                return GetDirectoryEntry(objectName, DsNameFormat.DistinguishedName);
            }
            else
            {
                string dn = GcGetDnFromAttributeSearch("samAccountName", objectName, objectClass);

                if (dn == null)
                {
                    throw new ObjectNotFoundException($"An object {objectName} of type {objectClass} was not found in the global catalog");
                }

                return GetDirectoryEntry(dn, DsNameFormat.DistinguishedName);
            }
        }

        private string GcGetDnFromAttributeSearch(string attributeName, string attributeValue, string objectClass)
        {
            return this.discoveryServices.FindGcAndExecuteWithRetry(Forest.GetCurrentForest().Name, dc =>
            {
                DirectorySearcher d = new DirectorySearcher
                {
                    SearchRoot = new DirectoryEntry($"GC://{dc}"),
                    SearchScope = SearchScope.Subtree,
                    Filter = $"(&(objectClass={objectClass})({attributeName}={EscapeSearchFilterParameter(attributeValue)}))"
                };

                d.PropertiesToLoad.Add("distinguishedName");

                using (SearchResultCollection result = d.FindAll())
                {
                    if (result.Count > 1)
                    {
                        throw new AmbiguousNameException($"There was more than one value in the directory that matched the criteria of ({attributeName}={attributeValue})");
                    }

                    if (result.Count == 0)
                    {
                        return null;
                    }

                    return result[0].Properties["distinguishedName"][0].ToString();
                }
            });
        }

        private DirectoryEntry GetDirectoryEntry(string nameToFind, DsNameFormat nameFormat)
        {
            return this.discoveryServices.FindDcAndExecuteWithRetry(dc =>
            {
                var result = NativeMethods.CrackNames(nameFormat, DsNameFormat.DistinguishedName, nameToFind, dc, null);

                return this.discoveryServices.FindDcAndExecuteWithRetry(result.Domain, dc2 =>
                {
                    var de = new DirectoryEntry($"LDAP://{dc2}/{result.Name}");
                    _ = de.Guid;
                    return de;
                });
            });
        }

        private DirectoryEntry GetDirectoryEntry(SecurityIdentifier nameToFind)
        {
            return GetDirectoryEntry(nameToFind.ToString(), DsNameFormat.SecurityIdentifier);
        }

        private bool CheckForSidInToken(SecurityIdentifier principalSid, SecurityIdentifier sidToCheck, SecurityIdentifier requestContext = null)
        {
            if (principalSid == null)
            {
                throw new ArgumentNullException(nameof(principalSid));
            }

            if (sidToCheck == null)
            {
                throw new ArgumentNullException(nameof(sidToCheck));
            }

            if (principalSid == sidToCheck)
            {
                return true;
            }

            if (requestContext == null || requestContext.IsEqualDomainSid(CurrentDomainSid))
            {
                using (AuthorizationContext context = new AuthorizationContext(principalSid))
                {
                    return context.ContainsSid(sidToCheck);
                }
            }
            else
            {
                string dnsDomain = discoveryServices.GetDomainNameDns(requestContext.AccountDomainSid);

                return this.discoveryServices.Find2012DcAndExecuteWithRetry(dnsDomain, dc =>
                {
                    using (AuthorizationContext context = new AuthorizationContext(principalSid, dc))
                    {
                        return context.ContainsSid(sidToCheck);
                    }
                });
            }
        }

        private IEnumerable<SecurityIdentifier> GetTokenGroups(SecurityIdentifier principalSid, SecurityIdentifier requestContext = null)
        {
            if (principalSid == null)
            {
                throw new ArgumentNullException(nameof(principalSid));
            }

            if (requestContext == null || requestContext.IsEqualDomainSid(CurrentDomainSid))
            {
                using (AuthorizationContext context = new AuthorizationContext(principalSid))
                {
                    return context.GetTokenGroups().ToList(); // Force the enumeration now before the context goes out of scope
                }
            }
            else
            {
                string dnsDomain = discoveryServices.GetDomainNameDns(requestContext.AccountDomainSid);

                return this.discoveryServices.Find2012DcAndExecuteWithRetry(dnsDomain, dc =>
                {
                    using (AuthorizationContext context = new AuthorizationContext(principalSid, dc))
                    {
                        return context.GetTokenGroups().ToList(); // Force the enumeration now before the context goes out of scope
                    }
                });
            }
        }

        private string EscapeSearchFilterParameter(string p)
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
                _ = new X500DistinguishedName(name);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}