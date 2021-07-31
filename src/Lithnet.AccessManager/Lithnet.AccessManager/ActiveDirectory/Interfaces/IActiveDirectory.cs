using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager
{
    public interface IActiveDirectory
    {
        IActiveDirectoryGroup CreateTtlGroup(string accountName, string displayName, string description, string ou, string targetDc, TimeSpan ttl, GroupType groupType, bool removeAccountOperators);

        IActiveDirectoryComputer GetComputer(string name);

        IActiveDirectoryComputer GetComputer(SecurityIdentifier sid);

        bool TryGetComputer(string name, out IActiveDirectoryComputer computer);

        bool TryGetComputer(SecurityIdentifier sid, out IActiveDirectoryComputer computer);
        void DeleteGroup(string name);

        IActiveDirectoryGroup GetGroup(string groupName);

        IActiveDirectoryGroup GetGroup(SecurityIdentifier sid);

        void CreateGroup(string name, string description, GroupType groupType, string ou, bool removeAccountOperators);

        bool TryGetGroup(SecurityIdentifier sid, out IActiveDirectoryGroup group);

        bool TryGetGroup(string name, out IActiveDirectoryGroup group);

        IActiveDirectorySecurityPrincipal GetPrincipal(string principalName);

        IActiveDirectorySecurityPrincipal GetPrincipal(SecurityIdentifier sid);
        
        bool TryGetPrincipal(string name, out IActiveDirectorySecurityPrincipal principal);

        bool TryGetPrincipal(SecurityIdentifier sid, out IActiveDirectorySecurityPrincipal principal);

        IActiveDirectoryUser GetUser(string userName);

        IActiveDirectoryUser GetUser(SecurityIdentifier sid);
        
        bool TryGetUserByAltSecurityIdentity(string altSecurityIdentityValue, out IActiveDirectoryUser user);

        bool TryGetUser(string name, out IActiveDirectoryUser user);

        bool IsObjectInOu(IActiveDirectoryObject computer, string ou);

        bool IsContainer(DirectoryEntry path);

        bool IsPamFeatureEnabled(SecurityIdentifier domainSid, bool forceRefresh);

        bool IsPamFeatureEnabled(string dnsDomain, bool forceRefresh);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, IActiveDirectorySecurityPrincipal principal);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, IActiveDirectorySecurityPrincipal principal, SecurityIdentifier targetDomainSid);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal,
            SecurityIdentifier targetDomainSid);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, SecurityIdentifier principal);

        bool IsSidInPrincipalToken(SecurityIdentifier sidToFind, IEnumerable<SecurityIdentifier> tokenSids);

        IEnumerable<SecurityIdentifier> GetTokenGroups(IActiveDirectorySecurityPrincipal principal, SecurityIdentifier targetDomainSid);

        IEnumerable<SecurityIdentifier> GetTokenGroups(IActiveDirectorySecurityPrincipal principal);

        string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat, string dnsDomainName);

        string TranslateName(string name, DsNameFormat nameFormat, DsNameFormat requiredFormat);
        bool CanAccountBeDelegated(SecurityIdentifier serviceAccount);
        bool IsAccountGmsa(SecurityIdentifier serviceAccount);
        IEnumerable<IActiveDirectoryComputer> GetComputers(string name);
    }
}