using Lithnet.AccessManager.Enterprise;

namespace Lithnet.AccessManager.Server.UI
{
    public class EnterpriseEditionBadgeViewModelFactory : IViewModelFactory<EnterpriseEditionBadgeViewModel, EnterpriseEditionBadgeModel>
    {
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IAmsLicenseManager licenseManager;

        public EnterpriseEditionBadgeViewModelFactory(IShellExecuteProvider shellExecuteProvider, IAmsLicenseManager licenseManager)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
        }

        public EnterpriseEditionBadgeViewModel CreateViewModel(EnterpriseEditionBadgeModel model)
        {
            return new EnterpriseEditionBadgeViewModel(this.shellExecuteProvider, this.licenseManager, model);
        }
    }
}
