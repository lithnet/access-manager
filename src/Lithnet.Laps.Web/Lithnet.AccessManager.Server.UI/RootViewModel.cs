using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Server.UI.Interop;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class RootViewModel : Screen
    {
        public RootViewModel()
        {
            this.DisplayName = "Lithnet Access Manager Service Configuration";
            NativeMethods.ShowContainerDialog(IntPtr.Zero);
        }
    }
}
