using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010e-0000-0000-C000-000000000046")]
	internal interface IDataObject
	{
		[PreserveSig]
		int GetData(ref FORMATETC pFormatEtc, ref STGMEDIUM b);

		void GetDataHere(ref FORMATETC pFormatEtc, ref STGMEDIUM b);

		[PreserveSig]
		int QueryGetData(IntPtr a);

		[PreserveSig]
		int GetCanonicalFormatEtc(IntPtr a, IntPtr b);

		[PreserveSig]
		int SetData(IntPtr a, IntPtr b, int c);

		[PreserveSig]
		int EnumFormatEtc(uint a, IntPtr b);

		[PreserveSig]
		int DAdvise(IntPtr a, uint b, IntPtr c, ref uint d);

		[PreserveSig]
		int DUnadvise(uint a);

		[PreserveSig]
		int EnumDAdvise(IntPtr a);
	}
}
