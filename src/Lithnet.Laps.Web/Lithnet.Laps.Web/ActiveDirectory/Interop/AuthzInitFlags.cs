using System;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
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
