using System;

namespace Lithnet.AccessManager
{
    public interface IMsMcsAdmPwdProvider
    {
        DateTime? GetExpiry(IActiveDirectoryComputer computer);

        MsMcsAdmPwdPassword GetPassword(IActiveDirectoryComputer computer, DateTime? newExpiry);
        
        void SetPassword(IActiveDirectoryComputer computer, string password, DateTime expiryDate);

        void ClearPassword(IActiveDirectoryComputer computer);
    }
}