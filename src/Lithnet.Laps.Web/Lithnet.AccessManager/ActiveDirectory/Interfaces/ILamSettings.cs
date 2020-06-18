using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface ILamSettings : IDirectoryObject
    {
        string JitGroupReference { get; }

        DateTime? PasswordExpiry { get; }

        IReadOnlyList<ProtectedPasswordHistoryItem> PasswordHistory { get; }

        void ReplacePasswordHistory(IList<ProtectedPasswordHistoryItem> items);

        void UpdateJitGroup(IGroup group);
    }
}