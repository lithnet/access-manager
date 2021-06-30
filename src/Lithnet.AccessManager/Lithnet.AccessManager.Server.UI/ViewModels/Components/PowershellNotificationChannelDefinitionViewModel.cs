using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class PowershellNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<PowershellNotificationChannelDefinition>
    {
        private readonly IScriptTemplateProvider scriptTemplateProvider;

        public PowershellNotificationChannelDefinitionViewModel(PowershellNotificationChannelDefinition model, IModelValidator<PowershellNotificationChannelDefinitionViewModel> validator, IFileSelectionViewModelFactory fileSelectionViewModelFactory, IAppPathProvider appPathProvider, IScriptTemplateProvider scriptTemplateProvider) :
            base(model)
        {
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.Script = fileSelectionViewModelFactory.CreateViewModel(model, () => model.Script, appPathProvider.ScriptsPath);
            this.Script.DefaultFileExtension = "ps1";
            this.Script.Filter = "PowerShell script|*.ps1";
            this.Script.NewFileContent = this.scriptTemplateProvider.WriteAuditLog;
            this.Script.PropertyChanged += Script_PropertyChanged;
            this.Validator = validator;
            this.Validate();
        }

        private void Script_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Validate();
        }

        public string ScriptFile => this.Script.File;

        public FileSelectionViewModel Script { get; }

        public int TimeOut { get => this.Model.TimeOut; set => this.Model.TimeOut = value; }
    }
}
