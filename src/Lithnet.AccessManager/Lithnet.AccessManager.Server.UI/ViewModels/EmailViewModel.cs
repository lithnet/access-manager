using System;
using System.Security.Cryptography;
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
        private readonly RandomNumberGenerator rng;
        private readonly IShellExecuteProvider shellExecuteProvider;


        public EmailViewModel(EmailOptions model, RandomNumberGenerator rng, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.model = model;
            this.rng = rng;
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

                    this.model.Password = new EncryptedData();
                    this.model.Password.Mode = 1;
                    byte[] salt = new byte[128];
                    rng.GetBytes(salt);
                    this.model.Password.Salt = Convert.ToBase64String(salt);
                    this.model.Password.Data = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), salt, DataProtectionScope.LocalMachine));
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
