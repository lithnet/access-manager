using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Server.UI.Interop
{
	[ComImport, Guid("9068270b-0939-11d1-8be1-00c04fd8d503")]
	internal interface IAdsLargeInteger
	{
		long HighPart { get; set; }

		long LowPart { get; set; }
	}
}
