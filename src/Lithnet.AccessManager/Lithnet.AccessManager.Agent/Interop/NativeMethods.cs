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
        public const int WTS_CURRENT_SERVER_HANDLE = 0;

        [DllImport(Lib.NetApi32, SetLastError = false, ExactSpelling = true)]
        public static extern HRESULT NetGetAadJoinInformation([MarshalAs(UnmanagedType.LPWStr), Optional] string pcszTenantId, out DsRegJoinInfo ppJoinInfo);

        [DllImport(Lib.NetApi32, SetLastError = false, ExactSpelling = true)]
        public static extern void NetFreeAadJoinInformation(HANDLE pJoinInfo);

        [DllImport("WTSApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool WTSEnumerateSessions(IntPtr hServer, int reserved, int version, out IntPtr ppSessionInfo, out int pCount);

        [DllImport("WTSApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("WTSApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool WTSQueryUserToken(int sessionId, out IntPtr phToken);
    }
}
