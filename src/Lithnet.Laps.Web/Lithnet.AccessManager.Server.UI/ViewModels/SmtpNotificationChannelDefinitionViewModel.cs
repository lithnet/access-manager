using System.Collections.Generic;
using System.IO;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class SmtpNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<SmtpNotificationChannelDefinition>
    {
        private readonly IAppPathProvider appPathProvider;

        public SmtpNotificationChannelDefinitionViewModel(SmtpNotificationChannelDefinition model, IModelValidator<SmtpNotificationChannelDefinitionViewModel> validator, INotificationSubscriptionProvider subscriptionProvider, IAppPathProvider appPathProvider)
            : base(model)
        {
            this.appPathProvider = appPathProvider;

            if (this.Model.EmailAddresses == null)
            {
                this.Model.EmailAddresses = new List<string>();
            }

            this.EmailAddresses = new BindableCollection<string>(this.Model.EmailAddresses);

            this.Validator = validator;
            this.Validate();
        }

        public BindableCollection<string> EmailAddresses { get; }

        public string TemplateFailure { get => this.Model.TemplateFailure; set => this.Model.TemplateFailure = value; }

        public string TemplateSuccess { get => this.Model.TemplateSuccess; set => this.Model.TemplateSuccess = value; }

        public string NewRecipient { get; set; }

        public string SelectedRecipient { get; set; }

        public string RecipientList => string.Join(", ", this.EmailAddresses);

        public void AddRecipient()
        {
            this.Model.EmailAddresses.Add(this.NewRecipient);
            this.EmailAddresses.Add(this.NewRecipient);
            this.ValidateProperty(nameof(this.EmailAddresses));
            this.NewRecipient = null;
        }

        public bool CanAddRecipient
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.NewRecipient))
                {
                    return false;
                }

                return this.ValidateProperty(nameof(this.NewRecipient));
            }
        }

        public void RemoveRecipient()
        {
            this.NewRecipient = this.SelectedRecipient;
            this.EmailAddresses.Remove(this.SelectedRecipient);
            this.Model.EmailAddresses.Remove(this.SelectedRecipient);
            this.ValidateProperty(nameof(this.EmailAddresses));
        }

        public bool CanRemoveRecipient => this.SelectedRecipient != null;

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
    }
}