using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("965FC360-16FF-11d0-91CB-00AA00BBB723")]
    public interface ISecurityInformation
    {
        [PreserveSig]
        int GetObjectInformation(ref SiObjectInfo object_info);

        [PreserveSig]
        int GetSecurity([In] SecurityInfos RequestInformation, out IntPtr SecurityDescriptor, [In, MarshalAs(UnmanagedType.Bool)] bool fDefault);

       
        [PreserveSig]
        int SetSecurity([In] SecurityInfos RequestInformation, [In] IntPtr SecurityDescriptor);

        [PreserveSig]
        int GetAccessRights([In] IntPtr guidObject, [In] int dwFlags, [MarshalAs(UnmanagedType.LPArray)] out SiAccess[] access, out int access_count, out int DefaultAccess);

        [PreserveSig]
        int MapGeneric(ref Guid guidObjectType, ref AceFlags AceFlags, ref int Mask);

        [PreserveSig]
        int GetInheritTypes(out IntPtr InheritType, out int InheritTypesCount);

        [PreserveSig]
        int PropertySheetPageCallback([In] IntPtr hwnd, [In] PropertySheetCallbackMessage uMsg, [In] SiPageType uPage);
    }
}