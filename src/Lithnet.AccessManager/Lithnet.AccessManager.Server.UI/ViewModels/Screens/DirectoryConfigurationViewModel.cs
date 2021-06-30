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
    public class DirectoryConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        public DirectoryConfigurationViewModel(ActiveDirectoryConfigurationViewModel adVm,  AzureAdConfigurationViewModel aadVm, AmsDirectoryConfigurationViewModel amsVm)
        {
            this.Items.Add(adVm);
            this.Items.Add(aadVm);
            this.Items.Add(amsVm);
            this.DisplayName = "Directory configuration";
        }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;
    }
}