using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Lithnet.AccessManager
{
    public interface IComputer
    {
        string Description { get; }

        string DisplayName { get; }

        string DnsHostName { get; }

        string Name { get; }

        string FullyQualifiedName { get; }

        string ObjectID { get; }

        string AuthorityId { get; }

        AuthorityType AuthorityType { get; }

        string AuthorityDeviceId { get; }

        SecurityIdentifier SecurityIdentifier { get; }
    }
}
