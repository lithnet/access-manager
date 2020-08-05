using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LsaUnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }
}
