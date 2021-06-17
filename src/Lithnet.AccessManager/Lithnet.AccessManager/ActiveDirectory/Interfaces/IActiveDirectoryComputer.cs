using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace Lithnet.AccessManager
{
    public interface IActiveDirectoryComputer : ISecurityPrincipal, IComputer
    {
        DirectoryEntry DirectoryEntry { get; }

        IEnumerable<Guid> GetParentGuids();
    }
}
