using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct AuthzRpcInitInfoClient
    {
        public AuthzRpcClientVersion Version;
        public string ObjectUuid;
        public string Protocol;
        public string Server;
        public string EndPoint;
        public string Options;
        public string ServerSpn;
    }
}