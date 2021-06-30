using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class HelpViewModel : Screen, IHelpLink
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public HelpViewModel(IShellExecuteProvider shellExecuteProvider)
        {
            this.DisplayName = "Help";
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public PackIconUniconsKind Icon => PackIconUniconsKind.QuestionCircle;

        public void GettingStarted() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkGettingStarted);

        public void InstallingAms() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkInstallingAms);
        
        public void InstallingAma() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkInstallingAma);

        public void SettingUpBitLocker() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkSettingUpBitLocker);

        public void SettingUpJit() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkSettingUpJit);

        public void SettingUpLaps() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkSettingUpLaps);

        public void SettingUpAma() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkSettingUpAma);

        public void Troubleshooting() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkTroubleshooting);

        public void Faqs() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkFaqs);
      
        public void SupportInformation() => this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkSupportInformation);

        public string HelpLink => Constants.HelpLinkPageHelp;
    }
}
