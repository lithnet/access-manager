using System;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<WebhookNotificationChannelDefinition>
    {
        public WebhookNotificationChannelDefinitionViewModel(WebhookNotificationChannelDefinition model, INotificationSubscriptionProvider subscriptionProvider)
            :base(model)
        {
            this.Validator = new FluentModelValidator<WebhookNotificationChannelDefinitionViewModel>(new WebhookNotificationChannelDefinitionValidator(subscriptionProvider));
            this.Validate();
        }

        public string ContentType { get => this.Model.ContentType; set => this.Model.ContentType = value; }

        public string HttpMethod { get => this.Model.HttpMethod; set => this.Model.HttpMethod = value; }

        public string Url { get => this.Model.Url; set => this.Model.Url = value; }

        public string UrlHost
        {
            get
            {
                try
                {
                    Uri u = new Uri(this.Url);
                    return u.Host;
                }
                catch { }

                return null;
            }
        }

        public string TemplateFailure { get => this.Model.TemplateFailure; set => this.Model.TemplateFailure = value; }

        public string TemplateSuccess { get => this.Model.TemplateSuccess; set => this.Model.TemplateSuccess = value; }

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
