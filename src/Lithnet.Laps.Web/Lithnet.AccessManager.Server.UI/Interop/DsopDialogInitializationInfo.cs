using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Lithnet.AccessManager.Server.UI.Interop;

namespace Lithnet.AccessManager.Server.UI
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DsopDialogInitializationInfo
    {
        /// <summary>
        /// Contains the size, in bytes, of the structure.
        /// </summary>
        public int Size;

        /// <summary>
        /// Pointer to a null-terminated Unicode string that contains the name of the target computer. The dialog box operates as if it is running on the target computer, using the target computer to determine the joined domain and enterprise. If this value is NULL, the target computer is the local computer.
        /// </summary>
        [MarshalAs(UnmanagedType.LPTStr)]
        public string TargetComputer;

        /// <summary>
        /// Specifies the number of elements in the aDsScopeInfos array.
        /// </summary>
        public int ScopeInfoCount;

        /// <summary>
        /// Pointer to an array of DSOP_SCOPE_INIT_INFO structures that describe the scopes from which the user can select objects. This member cannot be NULL and the array must contain at least one element because the object picker cannot operate without at least one scope.
        /// </summary>
        public IntPtr ScopeInfo;

        /// <summary>
        /// Flags that determine the object picker options. This member can be zero or a combination of one or more of the following flags.
        /// </summary>
        public DsopDialogInitializationOptions Options;

        /// <summary>
        /// Contains the number of elements in the apwzAttributeNames array. This member can be zero.
        /// </summary>
        public int AttributesToFetchCount;

        /// <summary>
        /// Pointer to an array of null-terminated Unicode strings that contain the names of the attributes to retrieve for each selected object. If cAttributesToFetch is zero, this member is ignored.
        /// </summary>
        public IntPtr AttributesToFetch;
    }
}
