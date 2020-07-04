using System.Windows;
using Lithnet.AccessManager.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionViewModel : ValidatingModelBase, IViewAware
    {
        public PowershellNotificationChannelDefinitionViewModel(PowershellNotificationChannelDefinition model)
        {
            this.Model = model;
            this.Validator = new FluentModelValidator<PowershellNotificationChannelDefinitionViewModel>(new PowershellNotificationChannelDefinitionValidator());
            this.Validate();
        }

        public PowershellNotificationChannelDefinition Model { get; }

        public bool Enabled { get => this.Model.Enabled; set => this.Model.Enabled = value; }

        public string DisplayName { get => this.Model.DisplayName; set => this.Model.DisplayName = value; }

        public string Id { get => this.Model.Id; set => this.Model.Id = value; }

        public bool Mandatory { get => this.Model.Mandatory; set => this.Model.Mandatory = value; }

        public string Script { get => this.Model.Script; set => this.Model.Script = value; }

        public int TimeOut { get => this.Model.TimeOut; set => this.Model.TimeOut = value; }

        public UIElement View { get; private set; }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

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
