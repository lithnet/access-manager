using Lithnet.AccessManager.Api;
using Stylet;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryLithnetLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AmsDirectoryLithnetLapsConfigurationViewModel(IShellExecuteProvider shellExecuteProvider, INotifyModelChangedEventPublisher eventPublisher, EncryptionCertificateComponentViewModel encryptionCertVm, PasswordPoliciesViewModel policiesVm)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.PasswordEncryption = encryptionCertVm;
            this.PasswordPolicies = policiesVm;
            this.DisplayName = "Lithnet LAPS";
        }

        public EncryptionCertificateComponentViewModel PasswordEncryption { get; set; }

        public PasswordPoliciesViewModel PasswordPolicies { get; set; }

        public string HelpLink => Constants.HelpLinkPageEmail;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
