using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    public static class Constants
    {
        public const string LinkDownloadAccessManagerAgent = "https://github.com/lithnet/access-manager/releases";
        public const string LinkDownloadMsLaps = "https://aka.ms/LAPS";
        public const string UrlProductVersionInfo = "https://lithnet.github.io/access-manager/version.json";
        public const string LinkGmsaInfo = "https://docs.microsoft.com/en-us/windows-server/security/group-managed-service-accounts/group-managed-service-accounts-overview";

        public const string EventSourceName = "Lithnet Access Manager Configuration Tool";
        public const string EventLogName = "Lithnet Access Manager";
        public const string FirewallRuleName = "Lithnet Access Manager Web Service (HTTP/HTTPS-In)"; // This value also needs to be updated in the installer
        public const string ServiceExeName = "Lithnet.AccessManager.Web.exe";
    }
}
