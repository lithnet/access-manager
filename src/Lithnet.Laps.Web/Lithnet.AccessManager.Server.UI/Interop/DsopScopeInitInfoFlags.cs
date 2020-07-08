using System;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum DsopScopeInitInfoFlags
    {
        /// <summary>
        /// The scope described by this structure is initially selected in the Look in drop-down list. Only one scope can specify this flag. If no scope specifies this flag, the initial scope is the first successfully created scope in the array of scopes passed to the IDsObjectPicker::Initialize method.
        /// </summary>
        DSOP_SCOPE_FLAG_STARTING_SCOPE = 0x00000001,


        /// <summary>
        /// The ADsPaths are converted to use the WinNT provider. For more information, see WinNT ADsPath.
        /// </summary>
        DSOP_SCOPE_FLAG_WANT_PROVIDER_WINNT = 0x00000002,


        /// <summary>
        /// The ADsPaths are converted to use the LDAP provider. For more information, see LDAP ADsPath.
        /// </summary>
        DSOP_SCOPE_FLAG_WANT_PROVIDER_LDAP = 0x00000004,

        /// <summary>
        /// The ADsPaths for objects selected from this scope are converted to use the GC provider.
        /// </summary>
        DSOP_SCOPE_FLAG_WANT_PROVIDER_GC = 0x00000008,

        /// <summary>
        /// The ADsPaths having an objectSid attribute are converted to the form LDAP://<SID=x> where x represents the hexadecimal digits of the objectSid attribute value.
        /// </summary>
        DSOP_SCOPE_FLAG_WANT_SID_PATH = 0x00000010,

        /// <summary>
        /// The ADsPaths for down-level, well-known SID objects are an empty string unless this flag is specified (For example; DSOP_DOWNLEVEL_FILTER_INTERACTIVE). If this flag is specified, the paths have the form 
        /// 
        /// WinNT://NT AUTHORITY/Interactive or WinNT://Creator owner.
        /// </summary>
        DSOP_SCOPE_FLAG_WANT_DOWNLEVEL_BUILTIN_PATH = 0x00000020,

        /// <summary>
        /// If the scope filter contains users, select the Users check box in the dialog.
        /// </summary>
        DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS = 0x00000040,

        /// <summary>
        /// If the scope filter contains groups, select the Groups check box in the dialog.
        /// </summary>
        DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS = 0x00000080,

        /// <summary>
        /// If the scope filter contains computers, select the Computers check box in the dialog.
        /// </summary>
        DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS = 0x00000100,

        /// <summary>
        /// If the scope filter contains contacts, select the Contacts check box in the dialog.
        /// </summary>
        DSOP_SCOPE_FLAG_DEFAULT_FILTER_CONTACTS = 0x00000200,

        /// <summary>
        /// If the scope filter contains service accounts, select the Service Accounts and Group Managed Service Accounts check boxes in the dialog
        /// </summary>
        DSOP_SCOPE_FLAG_DEFAULT_FILTER_SERVICE_ACCOUNTS = 0x00000400,

        /// <summary>
        /// If the scope filter contains password setting objects, select the Password Setting Objects check box in the dialog.
        /// </summary>
        DSOP_SCOPE_FLAG_DEFAULT_FILTER_PASSWORDSETTINGS_OBJECTS = 0x00000800,
    }
}
