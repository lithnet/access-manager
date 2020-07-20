using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    public sealed class SafeHGlobalMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeHGlobalMemoryHandle() : base(true) { }

        internal SafeHGlobalMemoryHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(handle);
                handle = IntPtr.Zero;
                return true;
            }
            
            return false;
        }
    }
}
