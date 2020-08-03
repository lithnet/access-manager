using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WorkstationInfo100
    {
        public int PlatformID;

        public string ComputerName;

        public string LanGroup;

        public int MajorVersion;

        public int MinorVersion;
    }
}
