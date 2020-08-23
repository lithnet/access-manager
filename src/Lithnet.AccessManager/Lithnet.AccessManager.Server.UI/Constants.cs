using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    public static class Constants
    {
        public const string UrlProductVersionInfo = "https://lithnet.github.io/access-manager/version.json";

        public const string LinkDownloadMsLaps = "https://aka.ms/LAPS";

        public const string LinkDownloadAccessManagerAgent = "https://l.lithnet.io/fwlink/plrejfry";
        public const string LinkGmsaInfo = "https://l.lithnet.io/fwlink/fmuvtbkw";
        
        public const string HelpLinkPageActiveDirectory = "https://l.lithnet.io/fwlink/jzbnxnbz";
        public const string HelpLinkPageAuditing = "https://l.lithnet.io/fwlink/hvsjlbde";
        public const string HelpLinkPageAuthentication = "https://l.lithnet.io/fwlink/fhqobzto";
        public const string HelpLinkPageAuthorization = "https://l.lithnet.io/fwlink/xqbabhna";
        public const string HelpLinkPageEmail = "https://l.lithnet.io/fwlink/pbdufrgo";
        public const string HelpLinkPageIPAddressDetection = "https://l.lithnet.io/fwlink/nnrxpyvu";
        public const string HelpLinkPageJitAccess = "https://l.lithnet.io/fwlink/ctekraup";
        public const string HelpLinkPageLocalAdminPasswords = "https://l.lithnet.io/fwlink/vcbvhmkj";
        public const string HelpLinkPageChooseLapsStrategy = "https://l.lithnet.io/fwlink/vcbvhmkj";

        public const string HelpLinkPageRateLimits = "https://l.lithnet.io/fwlink/zbllbamw";
        public const string HelpLinkPageBitLocker = "https://l.lithnet.io/fwlink/lixfecyh";
        public const string HelpLinkPageUserInterface = "https://l.lithnet.io/fwlink/cfsimvws";
        public const string HelpLinkPageWebHosting = "https://l.lithnet.io/fwlink/qxdzonfv";

        public const string HelpLinkAuthNSetupOkta = "https://l.lithnet.io/fwlink/qxdzonfv";
        public const string HelpLinkAuthNSetupAzureAD = "https://l.lithnet.io/fwlink/lqcuxctn";
        public const string HelpLinkAuthNSetupAdfs = "https://l.lithnet.io/fwlink/nynyrawq";

        public const string LinkGettingStarted = "https://l.lithnet.io/fwlink/yynjgebg";
        public const string LinkInstallingAms = "https://l.lithnet.io/fwlink/garieonh";
        public const string LinkInstallingAma = "https://l.lithnet.io/fwlink/obbzzwzc";
        public const string LinkSettingUpJit = "https://l.lithnet.io/fwlink/jcnpklbg";
        public const string LinkSettingUpLaps = "https://l.lithnet.io/fwlink/pxrglipl";
        public const string LinkSettingUpAma = "https://l.lithnet.io/fwlink/jzovgllg ";
        public const string LinkSettingUpBitLocker = "https://l.lithnet.io/fwlink/wnrtsfax";
        public const string LinkTroubleshooting = "https://l.lithnet.io/fwlink/oubfibxc";
        public const string LinkFaqs = "https://l.lithnet.io/fwlink/xuzzzbat";
        public const string LinkSupportInformation = "https://l.lithnet.io/fwlink/ktayuqvb";

        public const string EventSourceName = "Lithnet Access Manager Configuration Tool";
        public const string EventLogName = "Lithnet Access Manager";
        public const string FirewallRuleName = "Lithnet Access Manager Web Service (HTTP/HTTPS-In)"; // This value also needs to be updated in the installer
        public const string ServiceExeName = "Lithnet.AccessManager.Service.exe";
        public const string AppName = "Lithnet Access Manager Service (AMS) Configuration";
    }
}
