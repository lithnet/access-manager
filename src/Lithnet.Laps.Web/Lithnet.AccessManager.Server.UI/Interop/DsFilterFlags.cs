using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DsFilterFlags
    {
        public DsopUplevelFitlerFlags UpLevel;

        public DsopDownlevelFilterFlags DownLevel;
    }
}
