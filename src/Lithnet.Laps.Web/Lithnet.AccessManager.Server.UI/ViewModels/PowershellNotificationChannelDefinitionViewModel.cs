using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class PowershellNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<PowershellNotificationChannelDefinition>
    {
        public PowershellNotificationChannelDefinitionViewModel(PowershellNotificationChannelDefinition model, IModelValidator<PowershellNotificationChannelDefinitionViewModel> validator, IFileSelectionViewModelFactory fileSelectionViewModelFactory, IAppPathProvider appPathProvider) :
            base(model)
        {
            this.Script = fileSelectionViewModelFactory.CreateViewModel(model, () => model.Script, appPathProvider.ScriptsPath);
            this.Script.DefaultFileExtension = "ps1";
            this.Script.Filter = "PowerShell script|*.ps1";
            this.Script.NewFileContent = ScriptTemplates.AuditScriptTemplate;
            this.Script.PropertyChanged += Script_PropertyChanged;

            this.Validator = validator;
            this.Validate();
        }

        private void Script_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.ValidateProperty(nameof(this.Script));
        }

        public string ScriptFile => this.Script.File;

        public FileSelectionViewModel Script { get; }

        public int TimeOut { get => this.Model.TimeOut; set => this.Model.TimeOut = value; }
    }
}
