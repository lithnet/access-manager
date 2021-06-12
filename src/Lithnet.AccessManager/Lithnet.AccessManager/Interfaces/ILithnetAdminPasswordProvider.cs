using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface ILithnetAdminPasswordProvider
    {
        ProtectedPasswordHistoryItem GetCurrentPassword(IComputer computer, DateTime? newExpiry);

        IReadOnlyList<ProtectedPasswordHistoryItem> GetPasswordHistory(IComputer computer);
        
        DateTime? GetExpiry(IComputer computer);
        
        void UpdateCurrentPassword(IComputer computer, string password, DateTime rotationInstant, DateTime expiryDate, int maximumPasswordHistory, PasswordAttributeBehaviour msLapsBehaviour);

        bool HasPasswordExpired(IComputer computer, bool considerMsMcsAdmPwdExpiry);

        void ClearPasswordHistory(IComputer computer);
        
        void ClearPassword(IComputer computer);

        void UpdatePasswordExpiry(IComputer computer, DateTime expiry);
    }
}