using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<PowershellNotificationChannelDefinition>
    {
        private readonly IDialogCoordinator dialogCoordinator;

        public PowershellNotificationChannelDefinitionViewModel(PowershellNotificationChannelDefinition model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider) :
            base(model)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.Validator = new FluentModelValidator<PowershellNotificationChannelDefinitionViewModel>(new PowershellNotificationChannelDefinitionValidator(subscriptionProvider));
            this.Validate();

            this.Script = new FileSelectionViewModel(model, () => model.Script, AppPathProvider.ScriptsPath, dialogCoordinator);
            this.Script.DefaultFileExtension = "ps1";
            this.Script.Filter = "PowerShell script|*.ps1";
            this.Script.NewFileContent = ScriptTemplates.AuditScriptTemplate;
            this.Script.PropertyChanged += Script_PropertyChanged;
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
