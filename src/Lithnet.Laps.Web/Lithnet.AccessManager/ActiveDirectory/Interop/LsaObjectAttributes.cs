using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
