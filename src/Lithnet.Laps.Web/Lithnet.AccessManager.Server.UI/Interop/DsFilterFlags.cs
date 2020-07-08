using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DsFilterFlags
    {
        public DsopUplevelFitlerFlags UpLevel;

        public DsopDownlevelFilterFlags DownLevel;
    }
}
