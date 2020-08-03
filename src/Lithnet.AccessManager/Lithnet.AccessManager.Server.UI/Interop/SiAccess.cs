using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SiAccess
    {
        public IntPtr pGuid;

        public uint AccessMask;

        [MarshalAs(UnmanagedType.LPWStr)] 
        public string DisplayName;

        public InheritFlags InheritanceFlags;

        public SiAccess(uint accessMask, string displayName, InheritFlags flags)
        {
            this.AccessMask = accessMask;
            this.DisplayName = displayName;
            InheritanceFlags = flags;
            pGuid = IntPtr.Zero;
        }
    }
}