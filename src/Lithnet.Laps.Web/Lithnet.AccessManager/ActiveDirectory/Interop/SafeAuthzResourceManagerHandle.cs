using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Lithnet.AccessManager.Interop
{
    internal class SafeAuthzResourceManagerHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        [DllImport("authz.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzFreeResourceManager(IntPtr handle);

        public SafeAuthzResourceManagerHandle()
        : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return AuthzFreeResourceManager(this.handle);
        }
    }
}
