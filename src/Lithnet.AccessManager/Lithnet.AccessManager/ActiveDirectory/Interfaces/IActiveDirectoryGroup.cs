using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface IActiveDirectoryGroup : IActiveDirectorySecurityPrincipal
    {
        void RetargetToDc(string dc);

        void AddMember(IActiveDirectorySecurityPrincipal principal);

        void AddMember(IActiveDirectorySecurityPrincipal principal, TimeSpan ttl);

        TimeSpan? EntryTtl { get; }

        void ExtendTtl(TimeSpan ttl);

        IEnumerable<string> GetMemberDNs();

        IEnumerable<string> GetMemberTtlDNs();

        TimeSpan? GetMemberTtl(IActiveDirectoryUser user);

        void RemoveMember(IActiveDirectorySecurityPrincipal principal);

        void RemoveMembers();
    }
}
