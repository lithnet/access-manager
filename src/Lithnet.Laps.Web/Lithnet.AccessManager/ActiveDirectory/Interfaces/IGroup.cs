using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface IGroup : ISecurityPrincipal
    {
        void AddMember(ISecurityPrincipal principal);

        void AddMember(ISecurityPrincipal principal, TimeSpan ttl);

        IEnumerable<string> GetMemberDNs();
    }
}
