using System;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class EmailViewModel : Screen, IHelpLink
    {
        private readonly EmailOptions emailOptions;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IProtectedSecretProvider secretProvider;
        private readonly AdminNotificationOptions adminNotificationOptions;

        public EmailViewModel(EmailOptions emailOptions, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, IProtectedSecretProvider secretProvider, AdminNotificationOptions adminNotificationOptions)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.secretProvider = secretProvider;
            this.adminNotificationOptions = adminNotificationOptions;
            this.emailOptions = emailOptions;

            this.DisplayName = "Email";
            eventPublisher.Register(this);
        }

        public string HelpLink => Constants.HelpLinkPageEmail;

        [NotifyModelChangedProperty]
        public string FromAddress { get => this.emailOptions.FromAddress; set => this.emailOptions.FromAddress = value; }

        [NotifyModelChangedProperty]
        public int Port { get => this.emailOptions.Port; set => this.emailOptions.Port = value; }

        [NotifyModelChangedProperty]
        public string Host { get => this.emailOptions.Host; set => this.emailOptions.Host = value; }

        [NotifyModelChangedProperty]
        public bool UseSsl { get => this.emailOptions.UseSsl; set => this.emailOptions.UseSsl = value; }

        [NotifyModelChangedProperty]
        public bool UseSpecifiedCredentials { get => !this.emailOptions.UseDefaultCredentials; set => this.emailOptions.UseDefaultCredentials = !value; }

        [NotifyModelChangedProperty]
        public string Username { get => this.emailOptions.Username; set => this.emailOptions.Username = value; }

        [NotifyModelChangedProperty]
        public string Password
        {
            get => this.emailOptions.Password?.Data == null ? null : "-placeholder-";
            set
            {
                if (value != "-placeholder-")
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        this.emailOptions.Password = null;
                        return;
                    }

                    this.emailOptions.Password = this.secretProvider.ProtectSecret(value);
                }
            }
        }

        [NotifyModelChangedProperty]
        public string AdminNotificationRecipients
        {
            get => this.adminNotificationOptions.AdminAlertRecipients;
            set => this.adminNotificationOptions.AdminAlertRecipients = value;
        }

        [NotifyModelChangedProperty]
        public bool EnableCertificateExpiryAlerts { get => this.adminNotificationOptions.EnableCertificateExpiryAlerts; set => this.adminNotificationOptions.EnableCertificateExpiryAlerts = value; }

        [NotifyModelChangedProperty]
        public bool EnableNewVersionAlerts { get => this.adminNotificationOptions.EnableNewVersionAlerts; set => this.adminNotificationOptions.EnableNewVersionAlerts = value; }

        public PackIconUniconsKind Icon => PackIconUniconsKind.At;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
