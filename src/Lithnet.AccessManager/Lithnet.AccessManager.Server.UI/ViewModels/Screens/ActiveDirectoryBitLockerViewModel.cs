using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryBitLockerViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ILogger<ActiveDirectoryBitLockerViewModel> logger;
        private readonly IWindowManager windowManager;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;

        public ActiveDirectoryBitLockerViewModel(IDialogCoordinator dialogCoordinator, IWindowsServiceProvider windowsServiceProvider, IShellExecuteProvider shellExecuteProvider, IScriptTemplateProvider scriptTemplateProvider, ILogger<ActiveDirectoryBitLockerViewModel> logger, IWindowManager windowManager, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.logger = logger;
            this.windowManager = windowManager;
            this.externalDialogWindowFactory = externalDialogWindowFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.windowsServiceProvider = windowsServiceProvider;
            this.DisplayName = "BitLocker";
        }

        public string HelpLink => Constants.HelpLinkPageBitLocker;

        public async Task DelegateServicePermission()
        {
            try
            {
                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Modify the OU variable in this script, and run it with domain admin rights to assign permissions for the service account to be able to read BitLocker recovery passwords from the directory",
                    ScriptText = this.scriptTemplateProvider.GrantBitLockerRecoveryPasswordPermissions.Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceAccountSid().ToString(), StringComparison.OrdinalIgnoreCase)
                };

                var evm = this.externalDialogWindowFactory.CreateViewModel(vm);
                windowManager.ShowDialog(evm);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not delegate service permission");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
