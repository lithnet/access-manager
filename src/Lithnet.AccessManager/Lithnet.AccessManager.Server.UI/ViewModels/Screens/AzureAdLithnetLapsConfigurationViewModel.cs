using Stylet;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdLithnetLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public AzureAdLithnetLapsConfigurationViewModel(IShellExecuteProvider shellExecuteProvider, INotifyModelChangedEventPublisher eventPublisher, EncryptionCertificateComponentViewModel encryptionVm, PasswordPoliciesViewModel passwordPolicies, IViewModelFactory<EnterpriseEditionBannerViewModel, EnterpriseEditionBannerModel> enterpriseEditionViewModelFactory)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.PasswordEncryption = encryptionVm;
            this.PasswordPolicies = passwordPolicies;

            this.DisplayName = "Lithnet LAPS";
            eventPublisher.Register(this);

            this.EnterpriseEdition = enterpriseEditionViewModelFactory.CreateViewModel(new EnterpriseEditionBannerModel
            {
                RequiredFeature = Enterprise.LicensedFeatures.AzureAdDeviceSupport,
                Link = Constants.EnterpriseEditionLearnMoreLinkAzureAdDevices
            });
        }

        public EnterpriseEditionBannerViewModel EnterpriseEdition { get; set; }

        public EncryptionCertificateComponentViewModel PasswordEncryption { get; set; }

        public string HelpLink => Constants.HelpLinkPageEmail;

        public PasswordPoliciesViewModel PasswordPolicies { get; set; }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
