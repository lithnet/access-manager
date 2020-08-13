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

        public const string HelpLinkPageActiveDirectory = "https://github.com/lithnet/access-manager/wiki/Active-Directory-Page";
        public const string HelpLinkPageAuditing = "https://github.com/lithnet/access-manager/wiki/Auditing-Page";
        public const string HelpLinkPageAuthentication = "https://github.com/lithnet/access-manager/wiki/Authentication-Page";
        public const string HelpLinkPageAuthorization = "https://github.com/lithnet/access-manager/wiki/Authorization-Page";
        public const string HelpLinkPageEmail = "https://github.com/lithnet/access-manager/wiki/Email-Page";
        public const string HelpLinkPageIPAddressDetection = "https://github.com/lithnet/access-manager/wiki/IP-Address-Detection-Page";
        public const string HelpLinkPageJitAccess = "https://github.com/lithnet/access-manager/wiki/Jit-Access-Page";
        public const string HelpLinkPageLocalAdminPasswords = "https://github.com/lithnet/access-manager/wiki/Local-Admin-Passwords-Page";
        public const string HelpLinkPageRateLimits = "https://github.com/lithnet/access-manager/wiki/Rate-Limits-Page";
        public const string HelpLinkPageUserInterface = "https://github.com/lithnet/access-manager/wiki/User-Interface-Page";
        public const string HelpLinkPageWebHosting = "https://github.com/lithnet/access-manager/wiki/Web-Hosting-Page";

        public const string HelpLinkAuthNSetupOkta = "https://github.com/lithnet/access-manager/wiki/Setting-up-authentication-with-Okta";
        public const string HelpLinkAuthNSetupAzureAD = "https://github.com/lithnet/access-manager/wiki/Setting-up-authentication-with-Azure-AD";
        public const string HelpLinkAuthNSetupAdfs = "https://github.com/lithnet/access-manager/wiki/Setting-up-authentication-with-ADFS";

        public const string EventSourceName = "Lithnet Access Manager Configuration Tool";
        public const string EventLogName = "Lithnet Access Manager";
        public const string FirewallRuleName = "Lithnet Access Manager Web Service (HTTP/HTTPS-In)"; // This value also needs to be updated in the installer
        public const string ServiceExeName = "Lithnet.AccessManager.Web.exe";
    }
}
