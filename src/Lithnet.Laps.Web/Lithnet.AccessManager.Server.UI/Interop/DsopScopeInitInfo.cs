using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [StructLayout(LayoutKind.Sequential)]

    internal struct DsopScopeInitInfo
    {
        /// <summary>
        /// Contains the size, in bytes, of the structure.
        /// </summary>
        public int Size;

        /// <summary>
        /// Flags that indicate the scope types described by this structure. You can combine multiple scope types if all specified scopes use the same settings. 
        /// </summary>
        public DsopScopeTypeFlags ScopeType;

        /// <summary>
        /// Flags that indicate the format used to return ADsPath for objects selected from this scope. The flScope member can also indicate the initial scope displayed in the Look in drop-down list. This member can be a combination of the following flags.
        /// LDAP and Global Catalog(GC) paths can be converted to the WinNT ADsPath Syntax.GC paths can be converted to the LDAP format.WinNT objects having an objectSid attribute can be converted to the LDAP format if you specify the DSOP_SCOPE_FLAG_WANT_SID_PATH or DSOP_SCOPE_FLAG_WANT_PROVIDER_LDAP flags.No other conversions are legal.
        /// </summary>
        public DsopScopeInitInfoFlags InitInfo;

        /// <summary>
        /// Contains a DSOP_FILTER_FLAGS structure that indicates the types of objects presented to the user for this scope or scopes.
        /// </summary>
        public DsFilterFlags Filter;

        /// <summary>
        /// Pointer to a null-terminated Unicode string that contains the name of a domain controller of the domain to which the target computer is joined. This member is used only if the flType member contains the DSOP_SCOPE_TYPE_UPLEVEL_JOINED_DOMAIN flag. If that flag is not set, pwzDcName must be NULL.
        /// 
        /// 
        /// This member can be NULL even if the DSOP_SCOPE_TYPE_UPLEVEL_JOINED_DOMAIN flag is specified, in which case, the dialog box looks up the domain controller. This member enables you to name a specific domain controller in a multimaster domain.For example, an administrative application might make changes on a domain controller in a multimaster domain, and then open the object picker dialog box before the changes have been replicated on the other domain controllers.
        /// </summary>
        public string DomainControllerName;

        /// <summary>
        /// Reserved; must be NULL.
        /// </summary>
        public string AdsPath;

        /// <summary>
        /// Contains an HRESULT value that indicates the status of the specific scope. If the IDsObjectPicker::Initialize method successfully creates the scope, or scopes, specified by this structure, hr contains S_OK. Otherwise, hr contains an error code.
        /// 
        /// If IDsObjectPicker::Initialize returns S_OK, the hr members of all the specified DSOP_SCOPE_INIT_INFO structures also contain S_OK.
        /// </summary>
        public int Result;
    }
}
