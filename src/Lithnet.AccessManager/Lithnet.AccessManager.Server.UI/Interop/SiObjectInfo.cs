using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SiObjectInfo
    {
        public SiObjectInfoFlags dwFlags;

        public IntPtr hInstance;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszServerName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszObjectName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string pszPageTitle;

        public Guid guidObjectType;
    }
}