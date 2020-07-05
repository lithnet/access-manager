using System.Windows;
using Lithnet.AccessManager.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<PowershellNotificationChannelDefinition>
    {
        public PowershellNotificationChannelDefinitionViewModel(PowershellNotificationChannelDefinition model, INotificationSubscriptionProvider subscriptionProvider):
            base (model)
        {
            this.Validator = new FluentModelValidator<PowershellNotificationChannelDefinitionViewModel>(new PowershellNotificationChannelDefinitionValidator(subscriptionProvider));
            this.Validate();
        }

        public string Script { get => this.Model.Script; set => this.Model.Script = value; }

        public int TimeOut { get => this.Model.TimeOut; set => this.Model.TimeOut = value; }

        public void ShowScriptDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = "ps1";
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "PowerShell Script (*.ps1)|*.ps1";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog(Window.GetWindow(this.View)) == true)
            {
                this.Script = openFileDialog.FileName;
            }
        }
    }
}
