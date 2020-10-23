using System;

namespace Lithnet.AccessManager.Server
{
    public interface IJitAccessProvider
    {
        TimeSpan GrantJitAccess(IGroup group, IUser user, IComputer computer, bool canExtend, TimeSpan requestedExpiry, out Action undo);
    }
}