using System;

namespace Lithnet.AccessManager
{
    public interface IJitAccessProvider
    {
        TimeSpan GrantJitAccess(IGroup group, IUser user, IComputer computer, bool canExtend, TimeSpan requestedExpiry, out Action undo);
    }
}