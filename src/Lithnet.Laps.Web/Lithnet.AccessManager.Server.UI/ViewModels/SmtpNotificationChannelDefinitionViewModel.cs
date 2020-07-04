using System.Collections.Generic;
using System.Net.Mail;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionViewModel : ValidatingModelBase, IViewAware
    {
        public SmtpNotificationChannelDefinition Model { get; }

        public SmtpNotificationChannelDefinitionViewModel(SmtpNotificationChannelDefinition model)
        {
            this.Model = model;

            if (this.Model.EmailAddresses == null)
            {
                this.Model.EmailAddresses = new List<string>();
            }

            this.EmailAddresses = new BindableCollection<string>(this.Model.EmailAddresses);

            this.Validator = new FluentModelValidator<SmtpNotificationChannelDefinitionViewModel>(new SmtpNotificationChannelDefinitionValidator());
            this.Validate();
        }

        public bool Enabled { get => this.Model.Enabled; set => this.Model.Enabled = value; }

        public string DisplayName { get => this.Model.DisplayName; set => this.Model.DisplayName = value; }

        public string Id { get => this.Model.Id; set => this.Model.Id = value; }

        public bool Mandatory { get => this.Model.Mandatory; set => this.Model.Mandatory = value; }

        public BindableCollection<string> EmailAddresses { get; }

        public string TemplateFailure { get => this.Model.TemplateFailure; set => this.Model.TemplateFailure = value; }

        public string TemplateSuccess { get => this.Model.TemplateSuccess; set => this.Model.TemplateSuccess = value; }

        public string NewRecipient { get; set; }

        public string SelectedRecipient { get; set; }

        public void AddRecipient()
        {
            this.Model.EmailAddresses.Add(this.NewRecipient);
            this.EmailAddresses.Add(this.NewRecipient);
        }

        public bool CanAddRecipient
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.NewRecipient))
                {
                    return false;
                }

                try
                {
                    MailAddress m = new MailAddress(this.NewRecipient);
                    return true;
                }
                catch { }

                return false;
            }
        }

        public void RemoveRecipient()
        {
            this.NewRecipient = this.SelectedRecipient;
            this.EmailAddresses.Remove(this.SelectedRecipient);
            this.Model.EmailAddresses.Remove(this.SelectedRecipient);
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

        public UIElement View { get; private set; }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}
