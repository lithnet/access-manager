using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface ILamSettings : IDirectoryObject
    {
        string ApplicationName { get; }

        string Description { get; }

        string MsDsObjectReference { get; }

        IEnumerable<string> MsDsSettings { get; }
    }
}