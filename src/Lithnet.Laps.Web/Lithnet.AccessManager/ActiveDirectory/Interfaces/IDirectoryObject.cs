using System;

namespace Lithnet.AccessManager
{
    public interface IDirectoryObject
    {
        string DistinguishedName { get; }

        Guid? Guid { get; }
    }
}
