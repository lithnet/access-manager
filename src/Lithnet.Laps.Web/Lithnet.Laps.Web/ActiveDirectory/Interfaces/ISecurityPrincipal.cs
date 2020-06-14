using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public interface ISecurityPrincipal
    {
        string SamAccountName { get; }

        string DistinguishedName { get; }

        Guid? Guid { get; }

        SecurityIdentifier Sid { get; }

        string MsDsPrincipalName { get; }
    }
}
