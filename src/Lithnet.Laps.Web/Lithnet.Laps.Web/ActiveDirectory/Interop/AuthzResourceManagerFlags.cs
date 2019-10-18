using System;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    [Flags]
    internal enum AuthzResourceManagerFlags : uint
    {
        NO_AUDIT = 0x1,
    }
}