using System;
using System.Linq;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;
using System.Text;
using Lithnet.Laps.Web.ActiveDirectory.Interop;
using Lithnet.Laps.Web.Internal;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ActiveDirectory : IDirectory
    {
        private const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";

        private const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";

        private static Guid PamFeatureGuid = new Guid("ec43e873-cce8-4640-b4ab-07ffe4ab5bcd");

        private Dictionary<SecurityIdentifier, bool> PamEnabledDomainCache = new Dictionary<SecurityIdentifier, bool>();

        public IUser GetUser(string userName)
        {
            SearchResult user = this.DoGcLookup(userName, "user", ActiveDirectoryUser.PropertiesToGet);
            return user == null ? null : new ActiveDirectoryUser(user);
        }

        public IComputer GetComputer(string computerName)
        {
            SearchResult result = this.DoGcLookup(computerName, "computer", ActiveDirectoryComputer.PropertiesToGet);

            return result == null ? null : new ActiveDirectoryComputer(result);
        }

        public ISecurityPrincipal GetPrincipal(string principalName)
        {
            SearchResult result = this.DoGcLookup(principalName, "*", ActiveDirectoryComputer.PropertiesToGet);

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

            throw new UnsupportedPrincipalTypeException($"The object '{principalName}' was of an unknown type: {result.GetPropertyCommaSeparatedString("objectClass")}");
        }

        public PasswordData GetPassword(IComputer computer)
        {
            SearchResult searchResult = this.GetDirectoryEntry(computer.DistinguishedName, "computer", ActiveDirectory.AttrMsMcsAdmPwd, ActiveDirectory.AttrMsMcsAdmPwdExpirationTime);

            if (!searchResult.Properties.Contains(ActiveDirectory.AttrMsMcsAdmPwd))
            {
                return null;
            }

            return new PasswordData(searchResult.GetPropertyString(ActiveDirectory.AttrMsMcsAdmPwd), searchResult.GetPropertyDateTimeFromLong(ActiveDirectory.AttrMsMcsAdmPwdExpirationTime));
        }

        public void SetPasswordExpiryTime(IComputer computer, DateTime time)
        {
            DirectoryEntry entry = new DirectoryEntry($"LDAP://{computer.DistinguishedName}");
            entry.Properties[ActiveDirectory.AttrMsMcsAdmPwdExpirationTime].Value = time.ToFileTimeUtc().ToString();
            entry.CommitChanges();
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

        public bool IsComputerInOu(IComputer computer, string ou)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"GC://{ou}"),
                SearchScope = SearchScope.Subtree,
                Filter = $"objectGuid={computer.Guid.ToOctetString()}"
            };

            return d.FindOne() != null;
        }

        public IGroup GetGroup(string groupName)
        {
            SearchResult result = this.DoGcLookup(groupName, "group", ActiveDirectoryGroup.PropertiesToGet);
            return result == null ? null : new ActiveDirectoryGroup(result);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal)
        {
            return this.IsSidInPrincipalToken(sidToFindInToken, principal, principal.Sid.AccountDomainSid);
        }

        public bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid)
        {
            return NativeMethods.CheckForSidInToken(principal.Sid, sidToFindInToken, targetDomainSid);
        }

        //public IList<ISecurityPrincipal> GetGroupMembers(IGroup group)
        //{
        //    //var groupEntry =
        //    throw new NotImplementedException();
        //}

        //public IEnumerable<string> GetNestedMemberDNsFromGroup(IGroup group)
        //{
        //    HashSet<string> memberDNs = new HashSet<string>();
        //    this.GetNestedMemberDNsFromGroup(group.DistinguishedName, memberDNs);

        //    return memberDNs;
        //}

        //private void GetNestedMemberDNsFromGroup(string dn, HashSet<string> members)
        //{
        //    foreach (string member in this.GetMemberDNsFromGroup(dn))
        //    {
        //        if (members.Add(member))
        //        {
        //            this.GetNestedMemberDNsFromGroup(member, members);
        //        }
        //    }
        //}

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
            var groupEntry = new DirectoryEntry($"LDAP://{group.DistinguishedName}");

            groupEntry.Properties["member"].Add($"<TTL={ttl.TotalSeconds},<SID={principal.Sid}>>");
            groupEntry.CommitChanges();
        }

        public void AddGroupMember(IGroup group, ISecurityPrincipal principal)
        {
            var groupEntry = new DirectoryEntry($"LDAP://{group.DistinguishedName}");

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

            string dc = NativeMethods.GetDnsDomainNameFromSid(sid);

            var rootDse = new DirectoryEntry($"LDAP://{dc}/rootDSE");

            var configNamingContext = (string)rootDse.Properties["configurationNamingContext"]?.Value;

            if (configNamingContext == null)
            {
                throw new ObjectNotFoundException($"Configuration naming context lookup failed");
            }

            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"LDAP://{configNamingContext}"),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass=msDS-OptionalFeature)(msDS-OptionalFeatureGUID={PamFeatureGuid.ToOctetString()}))",
            };

            bool result = d.FindOne() != null;

            PamEnabledDomainCache.Add(domainSid, result);

            return result;
        }

        private SearchResult DoGcLookup(string objectName, string objectClass, IEnumerable<string> propertiesToGet)
        {
            string dn;

            if (objectClass.Equals("computer", StringComparison.OrdinalIgnoreCase) && !objectName.EndsWith("$"))
            {
                objectName += "$";
            }

            if (objectName.Contains("\\") || objectName.Contains("@") || objectName.TryParseAsSid(out _))
            {
                dn = NativeMethods.GetDn(objectName);
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

        private static string DoGcLookupFromSimpleName(string name, string objectClass)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"GC://{Forest.GetCurrentForest().Name}"),
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass={objectClass})(samAccountName={ActiveDirectory.EscapeSearchFilterParameter(name)}))"
            };

            d.PropertiesToLoad.Add("distinguishedName");

            SearchResultCollection result = d.FindAll();

            if (result.Count > 1)
            {
                throw new AmbiguousNameException($"There was more than one value in the directory that matched the name {name}");
            }

            if (result.Count == 0)
            {
                return null;
            }

            return result[0].Properties["distinguishedName"][0].ToString();
        }

        private SearchResult GetDirectoryEntry(string dn, string objectClass, params string[] propertiesToLoad)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"LDAP://{dn}"),
                SearchScope = SearchScope.Base,
                Filter = $"objectClass={objectClass}"
            };

            foreach (string prop in propertiesToLoad)
            {
                d.PropertiesToLoad.Add(prop);
            }

            return d.FindOne();
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
    }
}