using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface ILithnetAdminPasswordProvider
    {
        ProtectedPasswordHistoryItem GetCurrentPassword(IComputer computer, DateTime? newExpiry);

        IReadOnlyList<ProtectedPasswordHistoryItem> GetPasswordHistory(IComputer computer);
        
        DateTime? GetExpiry(IComputer computer);
        
        void UpdateCurrentPassword(IComputer computer, string encryptedPassword, DateTime rotationInstant, DateTime expiryDate, int maximumPasswordHistory);
        
        void ClearPasswordHistory(IComputer computer);
        
        void ClearPassword(IComputer computer);

        void UpdatePasswordExpiry(IComputer computer, DateTime expiry);
    }
}