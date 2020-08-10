using System;

namespace Lithnet.AccessManager
{
    [Flags]
    public enum MsMcsAdmPwdBehaviour
    {
        Ignore = 0,
        Populate = 1,
        Clear = 2,
    }
}
