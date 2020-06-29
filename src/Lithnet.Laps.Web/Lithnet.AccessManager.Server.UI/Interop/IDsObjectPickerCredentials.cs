using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [ComImport, Guid("e2d3ec9b-d041-445a-8f16-4748de8fb1cf"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IDsObjectPickerCredentials
    {
        [PreserveSig]
        int Initialize(ref DsopDialogInitializationInfo pInitInfo);

        [PreserveSig]
        int InvokeDialog(IntPtr hwnd, out IDataObject dataObject);

        [PreserveSig]
        int SetCredentials(string username, string password);
    }
}
