using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Luid
    {
        public uint LowPart;

        public uint HighPart;

        public static Luid NullLuid => new Luid { HighPart = 0, LowPart = 0 };
    }
}