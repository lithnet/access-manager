﻿using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace Lithnet.Laps.Web
{
    public sealed class Directory
    {
        public const string AttrSamAccountName = "samAccountName";
        public const string AttrMsMcsAdmPwd = "ms-Mcs-AdmPwd";
        public const string AttrMsMcsAdmPwdExpirationTime = "ms-Mcs-AdmPwdExpirationTime";

        internal bool IsPrincipalInOu(Principal p, string ou)
        {
            DirectorySearcher d = new DirectorySearcher();
            d.SearchRoot = new DirectoryEntry($"LDAP://{ou}");
            d.SearchScope = SearchScope.Subtree;
            d.Filter = $"objectGuid={ToOctetString(p.Guid)}";

            return d.FindOne() != null;
        }

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

        internal Principal GetPrincipal(string name)
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            return Principal.FindByIdentity(ctx, name);
        }

        internal Principal GetPrincipal(IdentityType type, string name)
        {
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
            return Principal.FindByIdentity(ctx, type, name);
        }

        internal bool IsPrincipalInGroup(Principal p, GroupPrincipal group)
        {
            if (group?.Sid == null || group.Sid.BinaryLength == 0)
            {
                return false;
            }

            byte[] groupSid = new byte[group.Sid.BinaryLength];
            group.Sid.GetBinaryForm(groupSid, 0);
            return IsPrincipalInGroup(p, groupSid);
        }

        private bool IsPrincipalInGroup(Principal p, byte[] groupSid)
        {
            SearchResult result = GetDirectoryEntry(p, "tokenGroups");

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

        internal SearchResult GetDirectoryEntry(Principal p, params string[] propertiesToLoad)
        {
            return GetDirectoryEntry(p.DistinguishedName, propertiesToLoad);
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
    }
}