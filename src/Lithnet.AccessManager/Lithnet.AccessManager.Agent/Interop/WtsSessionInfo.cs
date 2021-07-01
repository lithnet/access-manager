using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Agent.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WtsSessionInfo
    {
        public int SessionId;

        public string WinStationName;

        public WtsConnectState State;
    }
}