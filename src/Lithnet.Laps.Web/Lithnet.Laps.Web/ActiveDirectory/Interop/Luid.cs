using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct Luid
    {
        public uint LowPart;

        public uint HighPart;

        public static Luid NullLuid => new Luid { HighPart = 0, LowPart = 0 };
    }
}