using System;

namespace Lithnet.AccessManager
{
    public interface IActiveDirectoryObject
    {
        string Path { get; }

        string DistinguishedName { get; }

        Guid? Guid { get; }
    }
}
