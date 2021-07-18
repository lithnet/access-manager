using Lithnet.AccessManager.Enterprise;
using Stylet;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class EnterpriseEditionBannerViewModel : Screen
    {
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IAmsLicenseManager licenseManager;
        private readonly EnterpriseEditionBannerModel model;
       
        public EnterpriseEditionBannerViewModel(IShellExecuteProvider shellExecuteProvider, IAmsLicenseManager licenseManager, EnterpriseEditionBannerModel model)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
            this.model = model;

            this.licenseManager.OnLicenseDataChanged += delegate
            {
                this.NotifyOfPropertyChange(nameof(this.IsEnterpriseEdition));
                this.NotifyOfPropertyChange(nameof(this.ShowEnterpriseEditionBanner));
            };
        }

        public async Task LinkLearnMore()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.model?.Link ?? Constants.EnterpriseEditionLearnMoreLinkHa);
        }

        public string FeatureName => this.model?.FeatureName ?? "This";

        public bool IsEnterpriseEdition => this.licenseManager.IsEnterpriseEdition();

        public bool ShowEnterpriseEditionBanner => this.model.RequiredFeature == LicensedFeatures.None ?
            this.licenseManager.IsEvaluatingOrBuiltIn() || !this.licenseManager.IsEnterpriseEdition() :
            !this.licenseManager.IsFeatureCoveredByFullLicense(this.model.RequiredFeature);

    }
}