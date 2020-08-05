using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LsaObjectAttributes
    {
        public int Length;

        public IntPtr RootDirectory;

        public IntPtr ObjectName;

        public int Attributes;

        public IntPtr SecurityDescriptor;

        public IntPtr SecurityQualityOfService;
    }
}
