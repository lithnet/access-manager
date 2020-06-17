using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface ILamSettings : IDirectoryObject
    {
        string JitGroupReference { get; }

        DateTime? PasswordExpiry { get; }
        IReadOnlyList<PasswordHistoryEntry> PasswordHistory { get; }
    }
}