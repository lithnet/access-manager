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
        private readonly EmailOptions model;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IProtectedSecretProvider secretProvider;

        public EmailViewModel(EmailOptions model, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, IProtectedSecretProvider secretProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.secretProvider = secretProvider;
            this.model = model;
            this.DisplayName = "Email";
            eventPublisher.Register(this);
        }

        public string HelpLink => Constants.HelpLinkPageEmail;

        [NotifyModelChangedProperty]
        public string FromAddress { get => this.model.FromAddress; set => this.model.FromAddress = value; }

        [NotifyModelChangedProperty]
        public int Port { get => this.model.Port; set => this.model.Port = value; }

        [NotifyModelChangedProperty]
        public string Host { get => this.model.Host; set => this.model.Host = value; }

        [NotifyModelChangedProperty]
        public bool UseSsl { get => this.model.UseSsl; set => this.model.UseSsl = value; }

        [NotifyModelChangedProperty]
        public bool UseSpecifiedCredentials { get => !this.model.UseDefaultCredentials; set => this.model.UseDefaultCredentials = !value; }

        [NotifyModelChangedProperty]
        public string Username { get => this.model.Username; set => this.model.Username = value; }

        [NotifyModelChangedProperty]
        public string Password
        {
            get => this.model.Password?.Data == null ? null : "-placeholder-";
            set
            {
                if (value != "-placeholder-")
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        this.model.Password = null;
                        return;
                    }

                    this.model.Password = this.secretProvider.ProtectSecret(value);
                }
            }
        }

        public PackIconUniconsKind Icon => PackIconUniconsKind.At;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
