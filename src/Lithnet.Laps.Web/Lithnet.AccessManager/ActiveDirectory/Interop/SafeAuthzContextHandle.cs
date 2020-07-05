using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Lithnet.AccessManager.Interop
{
    internal class SafeAuthzContextHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzFreeContext(IntPtr authzClientContext);

        public SafeAuthzContextHandle()
        : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return AuthzFreeContext(this.handle);
        }
    }
}
