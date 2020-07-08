using System;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum DsopDownlevelFilterFlags : uint
    {
        /// <summary>
        /// Includes user objects.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_USERS = 0x80000001,

        /// <summary>
        /// Includes all local groups.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_LOCAL_GROUPS = 0x80000002,

        /// <summary>
        /// Includes all global groups.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_GLOBAL_GROUPS = 0x80000004,

        /// <summary>
        /// Includes computer objects.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_COMPUTERS = 0x80000008,

        /// <summary>
        /// Includes the well-known security principal "World (Everyone)", a group that includes all users.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_WORLD = 0x80000010,

        /// <summary>
        /// Includes the well-known security principal "Authenticated User", a group that includes all authenticated accounts in the target domain and its trusted domains.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_AUTHENTICATED_USER = 0x80000020,

        /// <summary>
        /// Includes the well-known security principal "Anonymous", which refers to null session logons.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_ANONYMOUS = 0x80000040,

        /// <summary>
        /// Includes the well-known security principal "Batch", which refers to batch server logons.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_BATCH = 0x80000080,

        /// <summary>
        /// Includes the well-known security principal "Creator Owner".
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_CREATOR_OWNER = 0x80000100,

        /// <summary>
        /// Includes the well-known security principal "Creator Group".
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_CREATOR_GROUP = 0x80000200,

        /// <summary>
        /// Includes the well-known security principal "Dialup".
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_DIALUP = 0x80000400,

        /// <summary>
        /// Includes the well-known security principal "Interactive", which refers to users who log on to interactively use the computer.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_INTERACTIVE = 0x80000800,

        /// <summary>
        /// Includes the well-known security principal "Network", which refers to network logons for high performance servers.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_NETWORK = 0x80001000,

        /// <summary>
        /// Includes the well-known security principal "Service", which refers to Win32 service logons.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_SERVICE = 0x80002000,

        /// <summary>
        /// Includes the well-known security principal "System", which refers to the LocalSystem account.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_SYSTEM = 0x80004000,

        /// <summary>
        /// Excludes local built-in groups returned by groups' enumeration.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_EXCLUDE_BUILTIN_GROUPS = 0x80008000,

        /// <summary>
        /// Includes the "Terminal Server" well-known security principal.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_TERMINAL_SERVER = 0x80010000,

        /// <summary>
        /// Includes all well-known security principals. This flag is the same as specifying all of the well-known security principal flags listed in this list. 
        /// 
        /// This flag should be used for forward compatibility because it causes any other down-level, well-known SIDs that might be added in the future your code to automatically be included.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_ALL_WELLKNOWN_SIDS = 0x80020000,

        /// <summary>
        /// Includes the "Local Service" well-known security principal.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_LOCAL_SERVICE = 0x80040000,

        /// <summary>
        /// Includes the "Network Service" well-known security principal.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_NETWORK_SERVICE = 0x80080000,

        /// <summary>
        /// Includes the "Remote Logon" well-known security principal.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_REMOTE_LOGON = 0x80100000,

        /// <summary>
        /// Includes the "Internet User" well-known security principal.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_INTERNET_USER = 0x80200000,

        /// <summary>
        /// Includes the "Owner Rights" well-known security principal.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_OWNER_RIGHTS = 0x80400000,

        /// <summary>
        /// Includes "Service SIDs" of all installed services.
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_SERVICES = 0x80800000,

        /// <summary>
        /// 
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_LOCAL_LOGON = 0x81000000,

        /// <summary>
        /// 
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_THIS_ORG_CERT = 0x82000000,

        /// <summary>
        /// 
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_IIS_APP_POOL = 0x84000000,

        /// <summary>
        /// 
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_ALL_APP_PACKAGES = 0x88000000,

        /// <summary>
        /// 
        /// </summary>
        DSOP_DOWNLEVEL_FILTER_LOCAL_ACCOUNTS = 0x90000000,
    }
}