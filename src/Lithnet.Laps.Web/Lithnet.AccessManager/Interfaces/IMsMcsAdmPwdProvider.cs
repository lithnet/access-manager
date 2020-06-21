using System;

namespace Lithnet.AccessManager
{
    public interface IMsMcsAdmPwdProvider
    {
        DateTime? GetExpiry(IComputer computer);

        MsMcsAdmPwdPassword GetPassword(IComputer computer, DateTime? newExpiry);
        
        void SetPassword(IComputer computer, string password, DateTime expiryDate);
    }
}