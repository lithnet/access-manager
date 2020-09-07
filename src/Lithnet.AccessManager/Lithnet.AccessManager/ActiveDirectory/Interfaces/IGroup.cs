using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface IGroup : ISecurityPrincipal
    {
        void RetargetToDc(string dc);

        void AddMember(ISecurityPrincipal principal);

        void AddMember(ISecurityPrincipal principal, TimeSpan ttl);

        TimeSpan? EntryTtl { get; }

        void ExtendTtl(TimeSpan ttl);

        IEnumerable<string> GetMemberDNs();

        IEnumerable<string> GetMemberTtlDNs();

        TimeSpan? GetMemberTtl(IUser user);

        void RemoveMember(ISecurityPrincipal principal);

        void RemoveMembers();
    }
}
