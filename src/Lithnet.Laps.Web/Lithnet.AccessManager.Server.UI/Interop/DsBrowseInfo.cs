using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Server.UI.Interop
{
	[StructLayout(LayoutKind.Sequential)]
	public struct DSBrowseInfo
	{
		/// <summary>
		/// Contains the size, in bytes, of the DSBROWSEINFO structure. This is used by the DsBrowseForContainer function for versioning purposes.
		/// </summary>
		public int StructSize;

		/// <summary>
		/// Handle of the window used as the parent of the container browser dialog box.
		/// </summary>
		public IntPtr DialogOwner;

		/// <summary>
		/// Pointer to a null-terminated string that contains the caption of the dialog box. If this member is NULL, a default caption is used.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string DialogCaption;

		/// <summary>
		/// Pointer to a null-terminated string that contains additional text to be displayed in the dialog box above the tree control. If this member is NULL, no additional text is displayed.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)] 
		public string TreeViewTitle;

		/// <summary>
		/// Pointer to a null-terminated Unicode string that contains the ADsPath of the container placed at the root of the dialog box. The user cannot navigate above this level using the dialog box.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string RootPath;

		/// <summary>
		/// Pointer to a null-terminated Unicode string that receives the ADsPath of the container selected in the dialog. This string will always be null-terminated even if cchPath is not large enough to hold the entire path. If dwFlags contains the DSBI_EXPANDONOPEN flag, this member contains the ADsPath of the container that should be initially selected in the dialog box.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Path;

		/// <summary>
		/// Contains the size, in WCHAR characters, of the pszPath buffer.
		/// </summary>
		public int PathSize;

		/// <summary>
		/// Contains a set of flags that define the behavior of the dialog box. This can be zero or a combination of one or more of the following values.
		/// </summary>
		public DsBrowseInfoFlags Flags;

		/// <summary>
		/// Pointer to an application-defined BFFCallBack callback function that receives notifications from the container browser dialog box. Set this member to NULL if it is not used.
		/// </summary>
		public IntPtr Callback;

		/// <summary>
		/// Contains an application-defined 32-bit value passed as the lpData parameter in all calls to pfnCallback. This member is ignored if pfnCallback is NULL.
		/// </summary>
		public IntPtr CallbackParameter;

		/// <summary>
		/// Contains one of the ADS_FORMAT_ENUM values that specifies the format that the ADSI path returned in pszPath will accept.
		/// </summary>
		public AdsFormat ReturnFormat;

		/// <summary>
		/// Pointer to a Unicode string that contains the user name used for the credentials. This member is ignored if dwFlags does not have the DSBI_HASCREDENTIALS flag set. If this member is NULL, the currently logged on user name is used.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string UserName;

		/// <summary>
		/// Pointer to a Unicode string that contains the password used for the credentials. This member is ignored if dwFlags does not have the DSBI_HASCREDENTIALS flag set. If this member is NULL, the password of the currently logged on user is used.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Password;

		/// <summary>
		/// Pointer to a Unicode string buffer that receives the class string of the selected. This member is ignored if dwFlags does not have the DSBI_RETURNOBJECTCLASS flag set.
		/// </summary>
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ObjectClass;

		/// <summary>
		/// Contains the size, in WCHAR characters, of the pszObjectClass buffer.
		/// </summary>
		public int ObjectClassSize;
	};
}
