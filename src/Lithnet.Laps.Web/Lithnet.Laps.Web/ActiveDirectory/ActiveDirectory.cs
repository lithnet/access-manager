using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;
using System.Text;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.ActiveDirectory.Interop;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ActiveDirectory : IDirectory
    {
        private const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";

        private const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";

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

            throw new InvalidOperationException($"The object '{principalName}' was of an unknown type");
        }

        public PasswordData GetPassword(IComputer computer)
        {
            SearchResult searchResult = this.GetDirectoryEntry(computer.DistinguishedName, ActiveDirectory.AttrMsMcsAdmPwd, ActiveDirectory.AttrMsMcsAdmPwdExpirationTime);

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
                SearchResult result = this.GetDirectoryEntry(path, "objectClass");

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

        public bool IsSidInPrincipalToken(SecurityIdentifier targetDomain, ISecurityPrincipal principal, SecurityIdentifier sidToCheck)
        {
            return NativeMethods.CheckForSidInToken(principal.Sid, sidToCheck, targetDomain);
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
                dn = NativeMethods.GetDnFromGc(objectName);
            }
            else
            {
                dn = ActiveDirectory.DoGcLookupFromSimpleName(objectName, objectClass);
            }

            if (dn == null)
            {
                throw new NotFoundException($"An object {objectName} of type {objectClass} was not found in the global catalog");
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
                throw new NotFoundException($"The object {dn} was not found in the directory or was not of the object class {objectClass}");
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

        private SearchResult GetDirectoryEntry(string dn, params string[] propertiesToLoad)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = new DirectoryEntry($"LDAP://{dn}"),
                SearchScope = SearchScope.Base,
                Filter = $"objectClass=*"
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