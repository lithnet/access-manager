using Stylet;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdLithnetLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AzureAdLithnetLapsConfigurationViewModel(IShellExecuteProvider shellExecuteProvider, INotifyModelChangedEventPublisher eventPublisher, EncryptionCertificateComponentViewModel encryptionVm, PasswordPoliciesViewModel passwordPolicies)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.PasswordEncryption = encryptionVm;
            this.PasswordPolicies = passwordPolicies;

            this.DisplayName = "Lithnet LAPS";
            eventPublisher.Register(this);
        }

        public EncryptionCertificateComponentViewModel PasswordEncryption { get; set; }

        public string HelpLink => Constants.HelpLinkPageEmail;

        public PasswordPoliciesViewModel PasswordPolicies { get; set; }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
