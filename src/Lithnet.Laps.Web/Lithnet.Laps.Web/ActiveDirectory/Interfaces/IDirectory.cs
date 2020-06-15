using Lithnet.Laps.Web.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Security.Principal;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public interface IDirectory
    {
        void AddGroupMember(IGroup group, ISecurityPrincipal principal);
        
        void AddGroupMember(IGroup group, ISecurityPrincipal principal, TimeSpan ttl);
        
        void CreateTtlGroup(string accountName, string displayName, string description, string ou, TimeSpan ttl);
        
        IComputer GetComputer(string computerName);
        
        IGroup GetGroup(string groupName);
        
        IEnumerable<string> GetMemberDNsFromGroup(IGroup group);
             
        ISecurityPrincipal GetPrincipal(string principalName);
        
        IUser GetUser(string userName);
        
        bool IsComputerInOu(IComputer computer, string ou);
        
        bool IsContainer(string path);
        
        bool IsPamFeatureEnabled(SecurityIdentifier domainSid);
        
        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal);
        
        bool IsSidInPrincipalToken(SecurityIdentifier sidToFindInToken, ISecurityPrincipal principal, SecurityIdentifier targetDomainSid);

        SearchResult GetDirectoryEntry(string dn, string objectClass, params string[] propertiesToLoad);
        
        SearchResult GetDirectoryEntry(ISecurityPrincipal principal, params string[] propertiesToLoad);

        SearchResult SearchDirectoryEntry(string basedn, string filter, SearchScope scope, params string[] propertiesToLoad);
    }
}