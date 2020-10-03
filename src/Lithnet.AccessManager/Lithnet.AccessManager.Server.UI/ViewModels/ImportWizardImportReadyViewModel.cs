using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardImportReadyViewModel : Screen
    {
        private readonly ILogger<ImportWizardImportReadyViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public ImportWizardImportReadyViewModel(ILogger<ImportWizardImportReadyViewModel> logger, IDialogCoordinator dialogCoordinator, IShellExecuteProvider shellExecuteProvider)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.shellExecuteProvider = shellExecuteProvider;
        }
    }
}
