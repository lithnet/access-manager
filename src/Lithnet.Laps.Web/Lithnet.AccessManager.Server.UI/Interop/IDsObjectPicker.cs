using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
	[ComImport, Guid("0C87E64E-3B7A-11D2-B9E0-00C04FD8DBF7"), 	 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IDsObjectPicker
	{
		[PreserveSig]
		int Initialize(ref DsopDialogInitializationInfo pInitInfo);

		[PreserveSig]
		int InvokeDialog(IntPtr hwnd, out IDataObject dataObject);
	}
}
