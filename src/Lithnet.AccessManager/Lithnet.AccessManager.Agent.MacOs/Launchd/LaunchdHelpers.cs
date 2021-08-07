using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Lithnet.Extensions.Hosting.Launchd
{
    public static class LaunchdHelpers
    {
        private static bool? _isLaunchdService;

        /// <summary>
        /// Check if the current process is hosted as a launchd Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a launchd Service, otherwise <c>false</c>.</returns>
        public static bool IsLaunchdService() => _isLaunchdService ?? (bool)(_isLaunchdService = CheckParentIsLaunchd());

        private static bool CheckParentIsLaunchd()
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return false;
            }

            try
            {
                // Check whether our direct parent is 'launchd'.
                var parentPid = GetParentPid();
                var process = Process.GetProcessById(parentPid);
                
                return parentPid == 1 && string.Equals(process.ProcessName, "launchd", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
            }

            return false;
        }

        [DllImport("libc", EntryPoint = "getppid")]
        private static extern int GetParentPid();
    }
}