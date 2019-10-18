using System;
using System.Runtime.InteropServices;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SidAndAttributes
    {
        public IntPtr Sid;

        public uint Attributes;
    }
}