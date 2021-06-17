using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager
{
    public interface IDirectory
    {
        IGroup CreateTtlGroup(string accountName, string displayName, string description, string ou, string targetDc, TimeSpan ttl, GroupType groupType, bool removeAccountOperators);

        IActiveDirectoryComputer GetComputer(string name);

        IActiveDirectoryComputer GetComputer(SecurityIdentifier sid);

        bool TryGetComputer(string name, out IActiveDirectoryComputer computer);

        bool TryGetComputer(SecurityIdentifier sid, out IActiveDirectoryComputer computer);
        void DeleteGroup(string name);

        IGroup GetGroup(string groupName);

        IGroup GetGroup(SecurityIdentifier sid);

        void CreateGroup(string name, string description, GroupType groupType, string ou, bool removeAccountOperators);

        bool TryGetGroup(SecurityIdentifier sid, out IGroup group);

        bool TryGetGroup(string name, out IGroup group);

        ISecurityPrincipal GetPrincipal(string principalName);

        ISecurityPrincipal GetPrincipal(SecurityIdentifier sid);
        
        bool TryGetPrincipal(string name, out ISecurityPrincipal principal);

        bool TryGetPrincipal(SecurityIdentifier sid, out ISecurityPrincipal principal);

        IUser GetUser(string userName);

        IUser GetUser(SecurityIdentifier sid);
        
        bool TryGetUserByAltSecurityIdentity(string altSecurityIdentityValue, out IUser user);

        bool TryGetUser(string name, out IUser user);

        bool IsObjectInOu(IDirectoryObject computer, string ou);

        bool IsContainer(DirectoryEntry path);

        bool IsPamFeatureEnabled(SecurityIdentifier domainSid, bool forceRefresh);

        bool IsPamFeatureEnabled(string dnsDomain, bool forceRefresh);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal,
            SecurityIdentifier targetDomainSid);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFind, IEnumerable<SecurityIdentifier> tokenSids);

        IEnumerable<SecurityIdentifier> GetTokenGroups(ISecurityPrincipal principal, SecurityIdentifier targetDomainSid);

        IEnumerable<SecurityIdentifier> GetTokenGroups(ISecurityPrincipal principal);

        string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat, string dnsDomainName);

        string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat);
        bool CanAccountBeDelegated(SecurityIdentifier serviceAccount);
        bool IsAccountGmsa(SecurityIdentifier serviceAccount);
    }
}