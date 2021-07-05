using Lithnet.AccessManager.Api;
using Stylet;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryLithnetLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AmsDirectoryLithnetLapsConfigurationViewModel(IShellExecuteProvider shellExecuteProvider, INotifyModelChangedEventPublisher eventPublisher, ApiAuthenticationOptions agentOptions,  EncryptionCertificateComponentViewModel encryptionCertVm)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.PasswordEncryption = encryptionCertVm;
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
