using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardCsvSettingsViewModel : Screen
    {
        private readonly ILogger<ImportWizardCsvSettingsViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;

        public ImportWizardCsvSettingsViewModel(ILogger<ImportWizardCsvSettingsViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<ImportWizardCsvSettingsViewModel> validator)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.Validator = validator;
            this.Validate();
        }

        public bool ImportFileHasHeaderRow { get; set; }
 
        public string ImportFile { get; set; }

        public async Task SelectImportFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = "csv";
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "CSV File (*.csv)|*.csv";
            openFileDialog.Multiselect = false;

            if (!string.IsNullOrWhiteSpace(this.ImportFile))
            {
                try
                {
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(this.ImportFile) ?? string.Empty;
                    openFileDialog.FileName = Path.GetFileName(this.ImportFile) ?? string.Empty;
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not determine file path");
                }
            }

            if (openFileDialog.ShowDialog(this.GetWindow()) == false)
            {
                return;
            }

            foreach (var line in File.ReadLines(openFileDialog.FileName).Skip(this.ImportFileHasHeaderRow ? 1 : 0))
            {
                if (line.Count(t => t == ',') < 1)
                {
                    await dialogCoordinator.ShowMessageAsync(this, "File format error", "The file was not in the expected format. View the help topic for this page for information on the correct format");
                    return;
                }
            }

            this.ImportFile = openFileDialog.FileName;
        }
    }
}
