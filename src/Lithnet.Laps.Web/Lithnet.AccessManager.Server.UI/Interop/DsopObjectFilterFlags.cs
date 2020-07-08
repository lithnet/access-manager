using System;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum DsopObjectFilterFlags
    {

        /// <summary>
        /// Includes objects that have the showInAdvancedViewOnly attribute set to TRUE.
        /// </summary>
        DSOP_FILTER_INCLUDE_ADVANCED_VIEW = 0x00000001,

        /// <summary>
        /// Includes user objects.
        /// </summary>
        DSOP_FILTER_USERS = 0x00000002,

        /// <summary>
        /// Includes built-in group objects. Built-in groups are group objects with a groupType value that contain the GROUP_TYPE_BUILTIN_LOCAL_GROUP (0x00000001), GROUP_TYPE_RESOURCE_GROUP (0x00000004), and GROUP_TYPE_SECURITY_ENABLED (0x80000000) flags.
        /// </summary>
        DSOP_FILTER_BUILTIN_GROUPS = 0x00000004,

        /// <summary>
        /// Includes the contents of the Well Known Security Principals container.
        /// </summary>
        DSOP_FILTER_WELL_KNOWN_PRINCIPALS = 0x00000008,

        /// <summary>
        /// Includes distribution group objects with universal scope.
        /// </summary>
        DSOP_FILTER_UNIVERSAL_GROUPS_DL = 0x00000010,

        /// <summary>
        /// Includes security groups with universal scope. This flag has no affect in a mixed mode domain because universal security groups do not exist in mixed mode domains.
        /// </summary>
        DSOP_FILTER_UNIVERSAL_GROUPS_SE = 0x00000020,

        /// <summary>
        /// Includes distribution group objects with global scope.
        /// </summary>
        DSOP_FILTER_GLOBAL_GROUPS_DL = 0x00000040,

        /// <summary>
        /// Includes security group objects with global scope.
        /// </summary>
        DSOP_FILTER_GLOBAL_GROUPS_SE = 0x00000080,

        /// <summary>
        /// Includes distribution group objects with domain local scope.
        /// </summary>
        DSOP_FILTER_DOMAIN_LOCAL_GROUPS_DL = 0x00000100,

        /// <summary>
        /// Includes security group objects with domain local scope.
        /// </summary>
        DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE = 0x00000200,

        /// <summary>
        /// Includes contact objects.
        /// </summary>
        DSOP_FILTER_CONTACTS = 0x00000400,

        /// <summary>
        /// Includes computer objects.
        /// </summary>
        DSOP_FILTER_COMPUTERS = 0x00000800,

        /// <summary>
        /// Includes managed service account and group managed service account objects.
        /// </summary>
        DSOP_FILTER_SERVICE_ACCOUNTS = 0x00001000,

        /// <summary>
        /// Includes password settings objects.
        /// </summary>
        DSOP_FILTER_PASSWORDSETTINGS_OBJECTS = 0x00002000
    }
}
