using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Api;
using MahApps.Metro.IconPacks;
using Stylet;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdLithnetLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly ApiAuthenticationOptions agentOptions;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AzureAdLithnetLapsConfigurationViewModel(IShellExecuteProvider shellExecuteProvider, INotifyModelChangedEventPublisher eventPublisher, ApiAuthenticationOptions agentOptions, EncryptionCertificateComponentViewModel encryptionVm)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.agentOptions = agentOptions;
            this.PasswordEncryption = encryptionVm;

            this.DisplayName = "Lithnet LAPS";
            eventPublisher.Register(this);
        }

        public EncryptionCertificateComponentViewModel PasswordEncryption { get; set; }

        public string HelpLink => Constants.HelpLinkPageEmail;

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAzureAdJoinedDevices
        {
            get => this.agentOptions.AllowAzureAdJoinedDeviceAuth;
            set => this.agentOptions.AllowAzureAdJoinedDeviceAuth = value;
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAzureAdRegisteredDevices
        {
            get => this.agentOptions.AllowAzureAdRegisteredDeviceAuth;
            set => this.agentOptions.AllowAzureAdRegisteredDeviceAuth = value;
        }

        // public PackIconUniconsKind Icon => PackIconUniconsKind.ServerConnection;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
