using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public interface IDirectory
    {
        SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType, SecurityIdentifier domainSid);

        SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType);

        void AddGroupMember(IGroup group, ISecurityPrincipal principal);

        void AddGroupMember(IGroup group, ISecurityPrincipal principal, TimeSpan ttl);

        void CreateTtlGroup(string accountName, string displayName, string description, string ou, TimeSpan ttl);

        IComputer GetComputer(string name);

        bool TryGetComputer(string name, out IComputer computer);

        IComputer GetComputer();

        void UpdateMsMcsAdmPwdAttribute(IComputer computer, string password, DateTime expiryDate);

        ILamSettings GetLamSettings(IComputer computer);

        ILamSettings GetOrCreateLamSettings(IComputer computer);

        void DeleteLamSettings(ILamSettings settings);

        byte[] TryGetLamPublicKey(SecurityIdentifier sid);

        bool TryGetLamSettings(IComputer computer, out ILamSettings lamSettings);

        string GetMachineNetbiosDomainName();

        IGroup GetGroup(string groupName);

        IGroup GetGroup(SecurityIdentifier sid);

        bool TryGetGroup(SecurityIdentifier sid, out IGroup group);

        bool TryGetGroup(string name, out IGroup group);

        IList<SecurityIdentifier> GetLocalGroupMembers(string name);

        void AddLocalGroupMember(string groupName, SecurityIdentifier member);

        void RemoveLocalGroupMember(string groupName, SecurityIdentifier member);

        IEnumerable<string> GetMemberDNsFromGroup(IGroup group);

        ISecurityPrincipal GetPrincipal(string principalName);

        bool TryGetPrincipal(string name, out ISecurityPrincipal principal);

        IUser GetUser(string userName);

        bool TryGetUser(string name, out IUser user);

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