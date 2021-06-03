using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public interface ISecurityPrincipal : IDirectoryObject
    {
        string SamAccountName { get; }

        SecurityIdentifier Sid { get; }

        string MsDsPrincipalName { get; }

        string Type { get; }
    }
}
