using System;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public interface IDevice
    {
        long Id { get; set; }

        string ObjectID { get; set; }

        string AgentVersion { get; set; }

        string ComputerName { get; set; }

        string DnsName { get; set; }

        DateTime Created { get; set; }

        DateTime Modified { get; set; }

        AuthorityType AuthorityType { get; set; }

        string Description { get; }

        string DisplayName { get; }

        string DnsHostName { get; }

        string Name { get; }

        string FullyQualifiedName { get; }

        string AuthorityId { get; set; }

        string AuthorityDeviceId { get; set; }

        SecurityIdentifier SecurityIdentifier { get; set; }

        ApprovalState ApprovalState { get; set; }

        string Sid { get; }

        string OperatingSystemFamily { get; set; }

        string OperatingSystemVersion { get; set; }
    }
}