using System.DirectoryServices;

namespace Lithnet.AccessManager
{
    public interface IComputer : ISecurityPrincipal
    {
        string Description { get; }

        string DisplayName { get; }

        DirectoryEntry DirectoryEntry { get; }
    }
}
