using System.Collections.Generic;

namespace Lithnet.AccessManager.Server
{
    public interface IBitLockerRecoveryPasswordProvider
    {
        IList<BitLockerRecoveryPassword> GetBitLockerRecoveryPasswords(IComputer computer);
    }
}