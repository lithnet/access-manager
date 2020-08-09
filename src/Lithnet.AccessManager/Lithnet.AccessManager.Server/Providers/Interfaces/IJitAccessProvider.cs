using System;

namespace Lithnet.AccessManager
{
    public interface IJitAccessProvider
    {
        TimeSpan GrantJitAccess(IGroup group, IUser user, bool canExtend, TimeSpan requestedExpiry, out Action undo);
    }
}