using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum DsopDialogInitializationOptions
    {
        /// <summary>
        /// If this flag is set, the user can select multiple objects. If this flag is not set, the user can select only one object.
        /// </summary>
        DSOP_FLAG_MULTISELECT = 0x00000001,

        /// <summary>
        /// If this flag is set and the DSOP_SCOPE_TYPE_TARGET_COMPUTER flag is set in the aDsScopeInfos array, the target computer is always included in the Look in drop-down list.
        /// 
        /// If this flag is not set and the target computer is an up-level or down-level domain controller, the DSOP_SCOPE_TYPE_TARGET_COMPUTER flag is ignored and the target computer is not included in the Look in drop-down list.
        /// 
        /// To save time during initialization, this flag should be set if it is known that the target computer is not a domain controller. However, if the target computer is a domain controller, this flag should not be set because it is better for the user to select domain objects from the domain scope rather than from the domain controller itself.
        /// </summary>
        DSOP_FLAG_SKIP_TARGET_COMPUTER_DC_CHECK = 0x00000002,
    }
}
