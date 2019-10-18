using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Models
{
    public interface ISecurityPrincipal
    {
        string SamAccountName { get; }

        string DistinguishedName { get; }

        Guid? Guid { get; }

        SecurityIdentifier Sid { get; }
    }
}
