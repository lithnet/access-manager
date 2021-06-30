using Lithnet.AccessManager.Enterprise;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryDevicesViewModel : Screen, IHelpLink
    {
        public AmsDirectoryDevicesViewModel()
        {
            this.DisplayName = "Devices";
        }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        //public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;
    }
}