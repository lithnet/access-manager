using System.Runtime.InteropServices;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TokenGroups
    {
        public uint GroupCount;

        [MarshalAs(UnmanagedType.ByValArray)]
        public SidAndAttributes[] Groups;
    }
}