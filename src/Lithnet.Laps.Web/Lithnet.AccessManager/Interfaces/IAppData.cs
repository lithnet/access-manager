using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface IAppData : IDirectoryObject
    {
        DateTime? PasswordExpiry { get; }

        IReadOnlyList<ProtectedPasswordHistoryItem> PasswordHistory { get; }

        ProtectedPasswordHistoryItem CurrentPassword { get; }

        void UpdateCurrentPassword(string encryptedPassword, DateTime rotationInstant, DateTime expiryDate, int maximumPasswordHistory);

        void ClearPasswordHistory();

        void UpdatePasswordExpiry(DateTime newExpiry);
    }
}