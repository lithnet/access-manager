using System.Windows;
using Lithnet.AccessManager.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionViewModel : ValidatingModelBase, IViewAware
    {
        public WebhookNotificationChannelDefinition Model { get; }

        public WebhookNotificationChannelDefinitionViewModel(WebhookNotificationChannelDefinition model)
        {
            this.Model = model;
            this.Validator = new FluentModelValidator<WebhookNotificationChannelDefinitionViewModel>(new WebhookNotificationChannelDefinitionValidator());
            this.Validate();
        }

        public bool Enabled { get => this.Model.Enabled; set => this.Model.Enabled = value; }

        public string DisplayName { get => this.Model.DisplayName; set => this.Model.DisplayName = value; }

        public string Id { get => this.Model.Id; set => this.Model.Id = value; }

        public bool Mandatory { get => this.Model.Mandatory; set => this.Model.Mandatory = value; }

        public string ContentType { get => this.Model.ContentType; set => this.Model.ContentType = value; }

        public string HttpMethod { get => this.Model.HttpMethod; set => this.Model.HttpMethod = value; }

        public string Url { get => this.Model.Url; set => this.Model.Url = value; }

        public string TemplateFailure { get => this.Model.TemplateFailure; set => this.Model.TemplateFailure = value; }

        public string TemplateSuccess { get => this.Model.TemplateSuccess; set => this.Model.TemplateSuccess = value; }

        public UIElement View { get; private set; }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public void ShowTemplateSuccessDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog(Window.GetWindow(this.View)) == true)
            {
                this.TemplateSuccess = openFileDialog.FileName;
            }
        }

        public void ShowTemplateFailureDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "All files (*.*)|*.*";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog(Window.GetWindow(this.View)) == true)
            {
                this.TemplateFailure = openFileDialog.FileName;
            }
        }

    }
}
