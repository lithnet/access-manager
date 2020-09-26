using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardImportReadyViewModel : Screen
    {
        private readonly ILogger<ImportWizardImportReadyViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;

        public ImportWizardImportReadyViewModel(ILogger<ImportWizardImportReadyViewModel> logger, IDialogCoordinator dialogCoordinator)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
        }
    }
}
