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
        private readonly IShellExecuteProvider shellExecuteProvider;

        public DirectoryConfigurationViewModel(ActiveDirectoryConfigurationViewModel adVm,  AzureAdConfigurationViewModel aadVm, AmsDirectoryConfigurationViewModel amsVm, IShellExecuteProvider shellExecuteProvider)
        {
            this.Items.Add(adVm);
            this.Items.Add(aadVm);
            this.Items.Add(amsVm);
            this.DisplayName = "Directory configuration";
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.SitemapSolid;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}