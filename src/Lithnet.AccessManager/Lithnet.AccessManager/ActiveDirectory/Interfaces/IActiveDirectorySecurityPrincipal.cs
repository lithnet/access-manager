using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public interface IActiveDirectorySecurityPrincipal : IActiveDirectoryObject
    {
        string SamAccountName { get; }

        SecurityIdentifier Sid { get; }

        string MsDsPrincipalName { get; }

        string Type { get; }
    }
}
