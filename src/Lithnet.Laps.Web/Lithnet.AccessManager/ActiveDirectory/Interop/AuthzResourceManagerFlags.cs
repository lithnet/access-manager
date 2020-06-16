using System;

namespace Lithnet.AccessManager.Interop
{
    [Flags]
    internal enum AuthzResourceManagerFlags : uint
    {
        NO_AUDIT = 0x1,
    }
}