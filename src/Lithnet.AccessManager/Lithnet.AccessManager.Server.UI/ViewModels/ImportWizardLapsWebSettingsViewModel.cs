using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardLapsWebSettingsViewModel : Screen
    {
        private readonly ILogger<ImportWizardLapsWebSettingsViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;

        public ImportWizardLapsWebSettingsViewModel(ILogger<ImportWizardLapsWebSettingsViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<ImportWizardLapsWebSettingsViewModel> validator)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.Validator = validator;
            this.Validate();
        }

        public string ImportFile { get; set; }

        public async Task SelectImportFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.DefaultExt = "config";
                openFileDialog.DereferenceLinks = true;
                openFileDialog.Filter = "web.config File (web.config)|web.config";
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

                string xml = await File.ReadAllTextAsync(openFileDialog.FileName);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                var nodes = doc.SelectNodes("/configuration/lithnet-laps/targets/target");
                if (nodes == null || nodes.Count == 0)
                {
                    await dialogCoordinator.ShowMessageAsync(this, "File format error", "There were no LAPS targets found in the specified config file");
                    return;
                }

                this.ImportFile = openFileDialog.FileName;
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to open file");
                await dialogCoordinator.ShowMessageAsync(this, "File open error", $"There was an error opening the LAPS web config file\r\n\r\n{ex.Message}");

            }
        }
    }
}
