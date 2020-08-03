using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    public static class Constants
    {
        public const string LinkDownloadAccessManagerAgent = "https://github.com/lithnet/laps-web/releases";
        public const string LinkDownloadMsLaps = "https://aka.ms/LAPS";
        public const string UrlProductVersionInfo = "https://lithnet.github.io/access-manager/version.json";
        public const string EventSourceName = "Lithnet Access Manager Configuration Tool";
        public const string EventLogName = "Lithnet Access Manager";
        public const string FirewallRuleName = "Lithnet Access Manager Web Service (HTTP/HTTPS-In)"; // This value also needs to be updated in the installer
        public const string ServiceExeName = "Lithnet.AccessManager.Web.exe";
    }
}
