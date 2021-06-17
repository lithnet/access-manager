using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface ILithnetAdminPasswordProvider
    {
        ProtectedPasswordHistoryItem GetCurrentPassword(IActiveDirectoryComputer computer, DateTime? newExpiry);

        IReadOnlyList<ProtectedPasswordHistoryItem> GetPasswordHistory(IActiveDirectoryComputer computer);
        
        DateTime? GetExpiry(IActiveDirectoryComputer computer);
        
        void UpdateCurrentPassword(IActiveDirectoryComputer computer, string password, DateTime rotationInstant, DateTime expiryDate, int maximumPasswordHistory, PasswordAttributeBehaviour msLapsBehaviour);

        bool HasPasswordExpired(IActiveDirectoryComputer computer, bool considerMsMcsAdmPwdExpiry);

        void ClearPasswordHistory(IActiveDirectoryComputer computer);
        
        void ClearPassword(IActiveDirectoryComputer computer);

        void UpdatePasswordExpiry(IActiveDirectoryComputer computer, DateTime expiry);
    }
}