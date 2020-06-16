using System;

namespace Lithnet.AccessManager.Interop
{
    [Flags]
    internal enum AuthzInitFlags : uint
    {
        Default = 0x0,
        SkipTokenGroups = 0x2,
        RequireS4ULogon = 0x4,
        ComputePrivileges = 0x8,
    }
}
