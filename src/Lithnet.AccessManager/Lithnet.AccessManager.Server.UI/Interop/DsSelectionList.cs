using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Server.UI.Interop
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct DsSelectionList
	{
		/// <summary>
		/// Contains the number of elements in the aDsSelection array.
		/// </summary>
		public uint Count;

		/// <summary>
		/// Contains the number of elements returned in the pvarFetchedAttributes member of each DS_SELECTION structure.
		/// </summary>
		public uint FetchedAttributeCount;
		
		/// <summary>
		/// Contains an array of DS_SELECTION structures, one for each object selected by the user. The cItems member indicates the number of elements in this array.
		/// </summary>
		public IntPtr DsSelectionItems;
	}
}
