using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public class LsaUnicodeString
	{
		public ushort Length;

		public ushort MaximumLength;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string Value;

		public LsaUnicodeString(string value)
		{
			Value = value;
			Length = (ushort)(Value.Length * 2);
			MaximumLength = Length;
		}

		public LsaUnicodeString()
		{
		}
	}
}
