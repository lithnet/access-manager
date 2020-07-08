using System;
using System.IO;
using Lithnet.AccessManager.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class WebhookNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<WebhookNotificationChannelDefinition>
    {
        private readonly IAppPathProvider appPathProvider;

        public WebhookNotificationChannelDefinitionViewModel(WebhookNotificationChannelDefinition model, IModelValidator<WebhookNotificationChannelDefinitionViewModel> validator, INotificationSubscriptionProvider subscriptionProvider, IAppPathProvider appPathProvider)
            :base(model)
        {
            this.appPathProvider = appPathProvider;
            this.Validator = validator;
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
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "All files (*.*)|*.*";
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
    }
}
