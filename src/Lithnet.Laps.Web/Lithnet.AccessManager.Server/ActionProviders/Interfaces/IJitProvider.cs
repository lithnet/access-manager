using System;

namespace Lithnet.AccessManager
{
    public interface IJitProvider
    {
        void GrantJitAccess(IComputer computer, IGroup group, IUser user, TimeSpan expiry);
    }
}