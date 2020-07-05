using System.Collections.Generic;
using System.Net.Mail;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionViewModel : NotificationChannelDefinitionViewModel<SmtpNotificationChannelDefinition>
    {
        public SmtpNotificationChannelDefinitionViewModel(SmtpNotificationChannelDefinition model, INotificationSubscriptionProvider subscriptionProvider)
            : base(model)
        {
            if (this.Model.EmailAddresses == null)
            {
                this.Model.EmailAddresses = new List<string>();
            }

            this.EmailAddresses = new BindableCollection<string>(this.Model.EmailAddresses);

            this.Validator = new FluentModelValidator<SmtpNotificationChannelDefinitionViewModel>(new SmtpNotificationChannelDefinitionValidator(subscriptionProvider));
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