using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardLapsWebSettingsViewModel : Screen
    {
        private readonly ILogger<ImportWizardLapsWebSettingsViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAppPathProvider appPathProvider;

        public ImportWizardLapsWebSettingsViewModel(ILogger<ImportWizardLapsWebSettingsViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<ImportWizardLapsWebSettingsViewModel> validator, IAppPathProvider appPathProvider)
        {
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.Validator = validator;
            this.appPathProvider = appPathProvider;
            this.Validate();
        }

        public string ImportFile { get; set; }

        public bool ImportNotifications { get; set; }

        [DependsOn(nameof(ImportNotifications))]
        public string TemplateSuccess { get; set; }

        [DependsOn(nameof(ImportNotifications))]
        public string TemplateFailure { get; set; }

        public void ShowTemplateSuccessDialog()
        {
            this.TemplateSuccess = this.GetDialogResult(this.TemplateSuccess);
        }

        public void ShowTemplateFailureDialog()
        {
            this.TemplateFailure = this.GetDialogResult(this.TemplateSuccess);
        }

        private string GetDialogResult(string initialFile)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = "html";
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "HTML files (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*";
            openFileDialog.Multiselect = false;

            if (!string.IsNullOrWhiteSpace(initialFile))
            {
                try
                {
                    string builtPath = this.appPathProvider.GetFullPath(initialFile, this.appPathProvider.TemplatesPath);
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(builtPath);
                    openFileDialog.FileName = Path.GetFileName(builtPath);
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(openFileDialog.InitialDirectory))
            {
                openFileDialog.InitialDirectory = this.appPathProvider.TemplatesPath;
            }

            if (openFileDialog.ShowDialog(this.GetWindow()) == true)
            {
                return this.appPathProvider.GetRelativePath(openFileDialog.FileName, this.appPathProvider.TemplatesPath);
            }

            return initialFile;
        }

        public async Task SelectImportFile()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                openFileDialog.DefaultExt = "config";
                openFileDialog.DereferenceLinks = true;
                openFileDialog.Filter = "web.config File (web.config)|web.config|Config files (*.config)|*.config|All files (*.*)|*.*";
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
