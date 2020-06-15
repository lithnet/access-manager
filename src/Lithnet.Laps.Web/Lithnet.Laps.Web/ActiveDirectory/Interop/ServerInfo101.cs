using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ServerInfo101
    {
        public ServerPlatform PlatformId;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Name;

        public int VersionMajor;

        public int VersionMinor;

        public ServerTypes Type;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Comment;
    }
}
