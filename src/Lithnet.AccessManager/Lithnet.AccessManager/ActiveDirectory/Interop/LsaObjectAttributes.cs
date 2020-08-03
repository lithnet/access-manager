using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LsaObjectAttributes
    {
        public uint Length;

        public IntPtr RootDirectory;

        public LsaUnicodeString ObjectName;

        public uint Attributes;

        public IntPtr SecurityDescriptor;

        public IntPtr SecurityQualityOfService;
    }
}
