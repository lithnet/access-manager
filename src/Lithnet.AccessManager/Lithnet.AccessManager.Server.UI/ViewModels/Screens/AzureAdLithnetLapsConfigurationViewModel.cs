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
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AzureAdLithnetLapsConfigurationViewModel(IShellExecuteProvider shellExecuteProvider, INotifyModelChangedEventPublisher eventPublisher, EncryptionCertificateComponentViewModel encryptionVm)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.PasswordEncryption = encryptionVm;

            this.DisplayName = "Lithnet LAPS";
            eventPublisher.Register(this);
        }

        public EncryptionCertificateComponentViewModel PasswordEncryption { get; set; }

        public string HelpLink => Constants.HelpLinkPageEmail;

        // public PackIconUniconsKind Icon => PackIconUniconsKind.ServerConnection;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
