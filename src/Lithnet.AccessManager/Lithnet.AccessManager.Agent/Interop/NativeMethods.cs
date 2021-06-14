using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Lithnet.AccessManager.Agent.Providers;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Agent.Interop
{
    internal class NativeMethods
    {
        [DllImport(Lib.NetApi32, SetLastError = false, ExactSpelling = true)]
        public static extern HRESULT NetGetAadJoinInformation([MarshalAs(UnmanagedType.LPWStr), Optional] string pcszTenantId, out DSREG_JOIN_INFO ppJoinInfo);

        [DllImport(Lib.NetApi32, SetLastError = false, ExactSpelling = true)]
        public static extern void NetFreeAadJoinInformation(HANDLE pJoinInfo);
    }
}
