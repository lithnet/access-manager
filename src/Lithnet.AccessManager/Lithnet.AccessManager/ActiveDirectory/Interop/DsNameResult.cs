using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DsNameResult
    {
        public int cItems;
        public IntPtr rItems;
    }
}