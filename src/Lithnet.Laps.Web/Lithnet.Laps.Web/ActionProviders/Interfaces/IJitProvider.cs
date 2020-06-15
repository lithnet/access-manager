using System;
using System.Collections.Generic;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public interface IJitProvider
    {
        void GrantJitAccess(IComputer computer, IGroup group, IUser user, TimeSpan expiry);
    }
}