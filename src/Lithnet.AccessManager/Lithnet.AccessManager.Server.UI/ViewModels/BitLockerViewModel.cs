using System;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class BitLockerViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IServiceSettingsProvider serviceSettings;

        public BitLockerViewModel(IDialogCoordinator dialogCoordinator, IServiceSettingsProvider serviceSettings)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.serviceSettings = serviceSettings;
            this.DisplayName = "BitLocker";
        }

        public string HelpLink => Constants.HelpLinkPageBitLocker;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.HddRegular;

        public void DelegateServicePermission()
        {
            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Modify the OU variable in this script, and run it with domain admin rights to assign permissions for the service account to be able to read BitLocker recovery passwords from the directory",
                ScriptText = ScriptTemplates.GrantBitLockerRecoveryPasswordPermissions.Replace("{serviceAccount}", this.serviceSettings.GetServiceAccount().ToString(), StringComparison.OrdinalIgnoreCase)
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
    }
}
