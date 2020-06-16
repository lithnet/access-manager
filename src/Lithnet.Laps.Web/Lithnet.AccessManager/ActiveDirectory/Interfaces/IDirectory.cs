using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public interface IDirectory
    {
        void AddGroupMember(IGroup group, ISecurityPrincipal principal);

        void AddGroupMember(IGroup group, ISecurityPrincipal principal, TimeSpan ttl);

        void CreateTtlGroup(string accountName, string displayName, string description, string ou, TimeSpan ttl);

        IComputer GetComputer(string computerName);

        IComputer GetComputer();

        ILamSettings GetLamSettings(IComputer computer);

        void UpdateLamSettings(IComputer computer, IGroup group, IList<string> settings);

        void UpdateLamSettings(IComputer computer, IGroup group);

        string GetMachineNetbiosDomainName();

        void UpdateLamSettings(IComputer computer, IList<string> settings);

        IGroup GetGroup(string groupName);

        IGroup GetGroup(SecurityIdentifier sid);

        IList<SecurityIdentifier> GetLocalGroupMembers(string name);

        void AddLocalGroupMember(string groupName, SecurityIdentifier member);

        void RemoveLocalGroupMember(string groupName, SecurityIdentifier member);

        IEnumerable<string> GetMemberDNsFromGroup(IGroup group);

        ISecurityPrincipal GetPrincipal(string principalName);

        IUser GetUser(string userName);

        bool IsObjectInOu(IDirectoryObject computer, string ou);

        bool IsContainer(string path);

        bool IsDomainController();

        bool IsPamFeatureEnabled(SecurityIdentifier domainSid);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid);

        SearchResult GetDirectoryEntry(string dn, string objectClass, params string[] propertiesToLoad);

        SearchResult GetDirectoryEntry(ISecurityPrincipal principal, params string[] propertiesToLoad);

        SearchResult SearchDirectoryEntry(string basedn, string filter, SearchScope scope, params string[] propertiesToLoad);
    }
}