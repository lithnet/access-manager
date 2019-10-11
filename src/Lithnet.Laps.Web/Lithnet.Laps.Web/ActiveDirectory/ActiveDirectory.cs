using System;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Principal;
using System.Text;
using Lithnet.Laps.Web.Directory;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.ActiveDirectory.Interop;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ActiveDirectory : IDirectory
    {
        private const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";

        private const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";

        public IComputer GetComputer(string computerName)
        {
            SearchResult result = this.DoGcLookup(computerName, "computer", ComputerAdapter.PropertiesToGet);

            return result == null ? null : new ComputerAdapter(result);
        }

        public Password GetPassword(IComputer computer)
        {
            SearchResult searchResult = this.GetDirectoryEntry(computer.DistinguishedName, ActiveDirectory.AttrMsMcsAdmPwd, ActiveDirectory.AttrMsMcsAdmPwdExpirationTime);

            if (!searchResult.Properties.Contains(ActiveDirectory.AttrMsMcsAdmPwd))
            {
                return null;
            }

            return new Password(
                searchResult.GetPropertyString(ActiveDirectory.AttrMsMcsAdmPwd),
                searchResult.GetPropertyDateTimeFromLong(ActiveDirectory.AttrMsMcsAdmPwdExpirationTime)
            );
        }

        public void SetPasswordExpiryTime(IComputer computer, DateTime time)
        {
            DirectoryEntry entry = new DirectoryEntry($"LDAP://{computer.DistinguishedName}");
            entry.Properties[ActiveDirectory.AttrMsMcsAdmPwdExpirationTime].Value = time.ToFileTimeUtc().ToString();
            entry.CommitChanges();
        }

        public bool IsComputerInOu(IComputer computer, string ou)
        {
            DirectorySearcher d = new DirectorySearcher();
            d.SearchRoot = new DirectoryEntry($"GC://{ou}");
            d.SearchScope = SearchScope.Subtree;
            d.Filter = $"objectGuid={computer.Guid.ToOctetString()}";

            return d.FindOne() != null;
        }

        public IGroup GetGroup(string groupName)
        {
            SearchResult result = this.DoGcLookup(groupName, "group", GroupAdapter.PropertiesToGet);
            return result == null ? null : new GroupAdapter(result);
        }

        public bool IsComputerInGroup(IComputer computer, IGroup group)
        {
            return this.IsPrincipalInGroup(computer.DistinguishedName, group.Sid);
        }

        public bool IsUserInGroup(IUser user, IGroup group)
        {
            return this.IsPrincipalInGroup(user.DistinguishedName, group.Sid);
        }

        public IUser GetUser(string userName)
        {
            SearchResult user = this.DoGcLookup(userName, "user", UserAdapter.PropertiesToGet);
            return user == null ? null : new UserAdapter(user);
        }

        private SearchResult DoGcLookup(string objectName, string objectClass, string[] propertiesToGet)
        {
            string dn;

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
                throw new NotFoundException($"The object {objectName} was not found in the global catalog");
            }

            DirectorySearcher d = new DirectorySearcher();

            d.SearchRoot = new DirectoryEntry($"LDAP://{dn}");
            d.SearchScope = SearchScope.Base;
            d.Filter = $"(objectClass=*)";

            foreach (string prop in propertiesToGet)
            {
                d.PropertiesToLoad.Add(prop);
            }

            return d.FindOne() ?? throw new NotFoundException($"The object {dn} was not found in the directory");
        }

        private static string DoGcLookupFromSimpleName(string name, string objectClass)
        {
            DirectorySearcher d = new DirectorySearcher();
            d.SearchRoot = new DirectoryEntry($"GC://{Forest.GetCurrentForest().Name}");
            d.SearchScope = SearchScope.Subtree;
            d.Filter = $"(&(objectClass={objectClass})(name={ActiveDirectory.EscapeSearchFilterParameter(name)}))";
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

        private bool IsPrincipalInGroup(string distinguishedName, SecurityIdentifier groupSecurityIdentifier)
        {
            if (groupSecurityIdentifier == null || groupSecurityIdentifier.BinaryLength == 0)
            {
                return false;
            }

            byte[] groupSid = new byte[groupSecurityIdentifier.BinaryLength];
            groupSecurityIdentifier.GetBinaryForm(groupSid, 0);

            return this.IsPrincipalInGroup(distinguishedName, groupSid);
        }

        private bool IsPrincipalInGroup(string distinguishedName, byte[] groupSid)
        {
            if (groupSid == null)
            {
                return false;
            }

            SearchResult result = this.GetDirectoryEntry(distinguishedName, "tokenGroups");

            if (result == null)
            {
                return false;
            }

            foreach (byte[] value in result.Properties["tokenGroups"].OfType<byte[]>())
            {
                if (groupSid.SequenceEqual(value))
                {
                    return true;
                }
            }

            return false;
        }

        private SearchResult GetDirectoryEntry(string dn, params string[] propertiesToLoad)
        {
            DirectorySearcher d = new DirectorySearcher();
            d.SearchRoot = new DirectoryEntry($"LDAP://{dn}");
            d.SearchScope = SearchScope.Base;
            d.Filter = $"objectClass=*";
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