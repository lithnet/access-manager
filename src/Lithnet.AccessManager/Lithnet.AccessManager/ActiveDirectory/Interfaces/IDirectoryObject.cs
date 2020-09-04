using System;

namespace Lithnet.AccessManager
{
    public interface IDirectoryObject
    {
        string Path { get; }

        string DistinguishedName { get; }

        Guid? Guid { get; }
    }
}
