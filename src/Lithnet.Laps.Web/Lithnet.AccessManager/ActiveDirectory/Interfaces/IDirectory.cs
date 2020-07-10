using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager
{
    public interface IDirectory
    {
        void AddGroupMember(IGroup group, ISecurityPrincipal principal);

        void AddGroupMember(IGroup group, ISecurityPrincipal principal, TimeSpan ttl);

        void CreateTtlGroup(string accountName, string displayName, string description, string ou, TimeSpan ttl);

        IComputer GetComputer(string name);

        IComputer GetComputer(SecurityIdentifier sid);

        bool TryGetComputer(string name, out IComputer computer);

        IGroup GetGroup(string groupName);

        IGroup GetGroup(SecurityIdentifier sid);

        IGroup GetGroup(SecurityIdentifier groupSid, SecurityIdentifier domainSid);

        IGroup CreateGroup(string name, string description, int groupType, DirectoryEntry ou);

        bool TryGetGroup(SecurityIdentifier sid, out IGroup group);

        bool TryGetGroup(string name, out IGroup group);
     
        IEnumerable<string> GetMemberDNsFromGroup(IGroup group);

        ISecurityPrincipal GetPrincipal(string principalName);

        bool TryGetPrincipal(string name, out ISecurityPrincipal principal);

        IUser GetUser(string userName);

        IUser GetUser(SecurityIdentifier sid);

        bool TryGetUser(string name, out IUser user);

        bool IsObjectInOu(IDirectoryObject computer, string ou);

        bool IsContainer(string path);
        
        DirectoryEntry GetConfigurationNamingContext(SecurityIdentifier domain);

        DirectoryEntry GetConfigurationNamingContext(string dnsDomain);
        
        DirectoryEntry GetSchemaNamingContext(SecurityIdentifier domain);

        DirectoryEntry GetSchemaNamingContext(string dnsDomain);

        bool DoesSchemaAttributeExist(string dnsDomain, string attributeName);

        bool IsPamFeatureEnabled(SecurityIdentifier domainSid);

        bool IsPamFeatureEnabled(string dnsDomain);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal,
            SecurityIdentifier targetDomainSid);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal);

        SearchResult GetDirectoryEntry(string dn, string objectClass, params string[] propertiesToLoad);

        SearchResult GetDirectoryEntry(ISecurityPrincipal principal, params string[] propertiesToLoad);

        SearchResult SearchDirectoryEntry(string basedn, string filter, SearchScope scope, params string[] propertiesToLoad);

        string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat, string dnsDomainName);

        string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat);
    }
}