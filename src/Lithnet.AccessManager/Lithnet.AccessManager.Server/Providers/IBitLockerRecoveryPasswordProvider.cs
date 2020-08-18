using System.Collections.Generic;

namespace Lithnet.AccessManager
{
    public interface IBitLockerRecoveryPasswordProvider
    {
        IList<BitLockerRecoveryPassword> GetBitLockerRecoveryPasswords(IComputer computer);
    }
}