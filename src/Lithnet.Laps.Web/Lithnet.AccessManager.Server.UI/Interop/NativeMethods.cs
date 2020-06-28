using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    internal static class NativeMethods
    {
        public const int DSBI_ENTIREDIRECTORY = 0x00090000;

        public const int MAX_PATH = 256;

        [DllImport("dsuiext.dll", CharSet = CharSet.Unicode)]
        private static extern int DsBrowseForContainer(IntPtr pInfo);

        public static string ShowContainerDialog(IntPtr hwnd)
        {
            IntPtr pInfo = IntPtr.Zero;

            try
            {
                DSBrowseInfo info = new DSBrowseInfo();

                info.StructSize = Marshal.SizeOf(info);
                info.DialogOwner = hwnd;
                info.Path = new string(new char[MAX_PATH]);
                info.PathSize = info.Path.Length;
                info.Flags = DsBrowseInfoFlags.EntireDirectory | DsBrowseInfoFlags.ReturnFormat;
                info.ReturnFormat = AdsFormat.X500Dn;
                info.ObjectClass = new string(new char[MAX_PATH]);
                info.ObjectClassSize = info.ObjectClass.Length;

                IntPtr ps = Marshal.AllocHGlobal(Marshal.SizeOf<DSBrowseInfo>());
                Marshal.StructureToPtr(info, ps, false);

                int status = DsBrowseForContainer(ps);

                if (status == 1)
                {
                    DSBrowseInfo result = (DSBrowseInfo)Marshal.PtrToStructure(ps, typeof(DSBrowseInfo));
                    return result.Path;
                }

                return null;
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pInfo);
                }
            }
        }
    }
}
