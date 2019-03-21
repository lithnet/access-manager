using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using Lithnet.Laps.Web.Directory;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ActiveDirectory: IDirectory
    {
        public const string AttrSamAccountName = "samAccountName";
        public const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";
        public const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";

        internal ComputerPrincipal GetComputerPrincipal(string name)
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            return ComputerPrincipal.FindByIdentity(ctx, name);
        }

        internal GroupPrincipal GetGroupPrincipal(string name)
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            return GroupPrincipal.FindByIdentity(ctx, name);
        }

        internal SearchResult GetDirectoryEntry(string dn, params string[] propertiesToLoad)
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

        internal string ToOctetString(Guid? guid)
        {
            if (!guid.HasValue)
            {
                return null;
            }

            return $"\\{string.Join("\\", guid.Value.ToByteArray().Select(t => t.ToString("X2")))}";
        }

        public IComputer GetComputer(string computerName)
        {
            var principal = GetComputerPrincipal(computerName);

            return principal == null ? null : new ComputerAdapter(principal);
        }

        public Password GetPassword(IComputer computer)
        {
            SearchResult searchResult = GetDirectoryEntry(computer.DistinguishedName, ActiveDirectory.AttrSamAccountName, ActiveDirectory.AttrMsMcsAdmPwd, ActiveDirectory.AttrMsMcsAdmPwdExpirationTime);

            if (!searchResult.Properties.Contains(ActiveDirectory.AttrMsMcsAdmPwd))
            {
                return null;
            }

            return new Password(
                searchResult.GetPropertyString(Web.ActiveDirectory.ActiveDirectory.AttrMsMcsAdmPwd),
                searchResult.GetPropertyDateTimeFromLong(ActiveDirectory.AttrMsMcsAdmPwdExpirationTime)
            );
        }

        public void SetPasswordExpiryTime(IComputer computer, DateTime time)
        {
            var entry = new DirectoryEntry($"LDAP://{computer.DistinguishedName}");
            entry.Properties[ActiveDirectory.AttrMsMcsAdmPwdExpirationTime].Value = time.ToFileTimeUtc().ToString();
            entry.CommitChanges();
        }

        public bool IsComputerInOu(IComputer computer, string ou)
        {
            DirectorySearcher d = new DirectorySearcher();
            d.SearchRoot = new DirectoryEntry($"LDAP://{ou}");
            d.SearchScope = SearchScope.Subtree;
            d.Filter = $"objectGuid={ToOctetString(computer.Guid)}";

            return d.FindOne() != null;
        }

        public IGroup GetGroup(string groupName)
        {
            var principal = GetGroupPrincipal(groupName);

            return principal == null ? null : new GroupAdapter(principal);
        }

        public bool IsComputerInGroup(IComputer computer, IGroup group)
        {
            return IsPrincipalInGroup(computer.DistinguishedName, group.Sid);
        }

        public bool IsUserInGroup(IUser user, IGroup group)
        {
            return IsPrincipalInGroup(user.DistinguishedName, group.Sid);
        }

        public IUser GetUser(string userName)
        {
            var user = GetUserPrincipal(userName);

            return user == null ? null : new UserAdapter(user);
        }

        private bool IsPrincipalInGroup(string distinguishedName, byte[] groupSid)
        {
            SearchResult result = GetDirectoryEntry(distinguishedName, "tokenGroups");

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

        private bool IsPrincipalInGroup(string distinguishedName, SecurityIdentifier groupSecurityIdentifier)
        {
            if (groupSecurityIdentifier == null || groupSecurityIdentifier.BinaryLength == 0)
            {
                return false;
            }

            byte[] groupSid = new byte[groupSecurityIdentifier.BinaryLength];
            groupSecurityIdentifier.GetBinaryForm(groupSid, 0);

            return IsPrincipalInGroup(distinguishedName, groupSid);
        }

        internal UserPrincipal GetUserPrincipal(string name)
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            return UserPrincipal.FindByIdentity(ctx, name);
        }
    }
}