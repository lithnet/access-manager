using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class BitLockerViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        
            public BitLockerViewModel(IDialogCoordinator dialogCoordinator, IWindowsServiceProvider windowsServiceProvider, IShellExecuteProvider shellExecuteProvider, IScriptTemplateProvider scriptTemplateProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.windowsServiceProvider = windowsServiceProvider;
            this.DisplayName = "BitLocker";
        }

        public string HelpLink => Constants.HelpLinkPageBitLocker;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.HddRegular;

        public void DelegateServicePermission()
        {
            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Modify the OU variable in this script, and run it with domain admin rights to assign permissions for the service account to be able to read BitLocker recovery passwords from the directory",
                ScriptText = this.scriptTemplateProvider.GrantBitLockerRecoveryPasswordPermissions.Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceSid().ToString(), StringComparison.OrdinalIgnoreCase)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "Script",
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();
        }
        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
