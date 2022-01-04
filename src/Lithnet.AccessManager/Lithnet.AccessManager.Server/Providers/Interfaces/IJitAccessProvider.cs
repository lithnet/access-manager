using System;

namespace Lithnet.AccessManager.Server
{
    public interface IJitAccessProvider
    {
        TimeSpan GrantJitAccess(IActiveDirectoryGroup group, IActiveDirectoryUser user, IComputer computer, bool canExtend, TimeSpan requestedExpiry, out Action undo);

        TimeSpan GrantJitAccess(IActiveDirectoryGroup group, IActiveDirectoryUser user, bool canExtend, TimeSpan requestedExpiry, out Action undo);
    }
}