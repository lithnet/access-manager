using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct DsSelection
	{
		/// <summary>
		/// Pointer to a null-terminated Unicode string that contains the object's relative distinguished name (RDN).
		/// </summary>
		[MarshalAs(UnmanagedType.LPTStr)] 
		public string Name;

		/// <summary>
		/// Pointer to a null-terminated Unicode string that contains the object's ADsPath. The format of this string depends on the flags specified in the flScope member of the DSOP_SCOPE_INIT_INFO structure for the scope from which this object was selected.
		/// </summary>
		[MarshalAs(UnmanagedType.LPTStr)]
		public string AdsPath;

		/// <summary>
		/// Pointer to a null-terminated Unicode string that contains the value of the object's objectClass attribute.
		/// </summary>
		[MarshalAs(UnmanagedType.LPTStr)]
		public string ObjectClass;

		/// <summary>
		/// Pointer to a null-terminated Unicode string that contains the object's userPrincipalName attribute value. If the object does not have a userPrincipalName value, pwzUPN points to an empty string (L"").
		/// </summary>
		[MarshalAs(UnmanagedType.LPTStr)]
		public string Upn;

		/// <summary>
		/// Pointer to an array of VARIANT structures. Each VARIANT contains the value of an attribute of the selected object. The attributes retrieved are determined by the attribute names specified in the apwzAttributeNames member of the DSOP_INIT_INFO structure passed to the IDsObjectPicker::Initialize method. The order of attributes in the pvarFetchedAttributes array corresponds to the order of attribute names specified in the apwzAttributeNames array.
		/// The object picker dialog box may not be able to retrieve the requested attributes.If the attribute cannot be retrieved, the vt member of the VARIANT structure contains VT_EMPTY.
		/// </summary>
		public IntPtr Attributes;

		/// <summary>
		/// Contains one, or more, of the DSOP_SCOPE_TYPE_ that indicate the type of scope from which this object was selected. For more information, and a list of DSOP_SCOPE_TYPE_ flags, see the flType member of the DSOP_SCOPE_INIT_INFO structure.
		/// </summary>
		public DsopScopeTypeFlags ScopeType;
	}
}
