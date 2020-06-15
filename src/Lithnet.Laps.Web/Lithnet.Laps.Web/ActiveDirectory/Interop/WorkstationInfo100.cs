using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
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
