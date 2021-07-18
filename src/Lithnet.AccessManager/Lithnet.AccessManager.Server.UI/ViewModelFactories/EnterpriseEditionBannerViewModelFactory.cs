using Lithnet.AccessManager.Enterprise;

namespace Lithnet.AccessManager.Server.UI
{
    public class EnterpriseEditionBannerViewModelFactory : IViewModelFactory<EnterpriseEditionBannerViewModel, EnterpriseEditionBannerModel>
    {
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IAmsLicenseManager licenseManager;

        public EnterpriseEditionBannerViewModelFactory(IShellExecuteProvider shellExecuteProvider, IAmsLicenseManager licenseManager)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
        }

        public EnterpriseEditionBannerViewModel CreateViewModel(EnterpriseEditionBannerModel model)
        {
            return new EnterpriseEditionBannerViewModel(shellExecuteProvider, licenseManager, model);
        }
    }
}
