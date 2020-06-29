using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum DsopScopeTypeFlags
    {
        /// <summary>
        /// Computer specified by the pwzTargetComputer member of the DSOP_INIT_INFO structure.
        /// 
        /// If the target computer is an up-level or down-level domain controller, this flag is ignored unless the DSOP_FLAG_SKIP_TARGET_COMPUTER_DC_CHECK flag is set in the flOptions member of the DSOP_INIT_INFO structure.
        /// </summary>
        DSOP_SCOPE_TYPE_TARGET_COMPUTER = 0x00000001,


        /// <summary>
        /// An up-level domain to which the target computer is joined. If this flag is set, use the pwzDcName member to specify the name of a domain controller in the joined domain.
        /// </summary>
        DSOP_SCOPE_TYPE_UPLEVEL_JOINED_DOMAIN = 0x00000002,

        /// <summary>
        /// A down-level domain to which the target computer is joined.
        /// </summary>
        DSOP_SCOPE_TYPE_DOWNLEVEL_JOINED_DOMAIN = 0x00000004,

        /// <summary>
        /// All domains in the enterprise to which the target computer belongs. If the DSOP_SCOPE_TYPE_UPLEVEL_JOINED_DOMAIN scope is specified, then the DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN scope represents all domains in the enterprise except the joined domain.
        /// </summary>
        DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN = 0x00000008,

        /// <summary>
        /// A scope that contains objects from all domains in the enterprise. An enterprise can contain only up-level domains.
        /// </summary>
        DSOP_SCOPE_TYPE_GLOBAL_CATALOG = 0x00000010,

        /// <summary>
        /// All up-level domains external to the enterprise but trusted by the domain to which the target computer is joined.
        /// </summary>
        DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN = 0x00000020,

        /// <summary>
        /// All down-level domains external to the enterprise, but trusted by the domain to which the target computer is joined.
        /// </summary>
        DSOP_SCOPE_TYPE_EXTERNAL_DOWNLEVEL_DOMAIN = 0x00000040,

        /// <summary>
        /// The workgroup to which the target computer is joined. Applies only if the target computer is not joined to a domain.
        /// The only type of object that can be selected from a workgroup is a computer.
        /// </summary>
        DSOP_SCOPE_TYPE_WORKGROUP = 0x00000080,

        /// <summary>
        /// Enables the user to enter an up-level scope. If neither of the DSOP_SCOPE_TYPE_USER_ENTERED_* types is specified, the dialog box restricts the user to the scopes in the Look in drop-down list.
        /// </summary>
        DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE = 0x00000100,

        /// <summary>
        /// Enables the user to enter a down-level scope.
        /// </summary>
        DSOP_SCOPE_TYPE_USER_ENTERED_DOWNLEVEL_SCOPE = 0x00000200,
    }
}
