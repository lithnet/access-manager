using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetViewModelFactory : IAsyncViewModelFactory<SecurityDescriptorTargetViewModel, SecurityDescriptorTarget, SecurityDescriptorTargetViewModelDisplaySettings>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAppPathProvider appPathProvider;
        private readonly IViewModelFactory<NotificationChannelSelectionViewModel, AuditNotificationChannels> channelSelectionViewModelFactory;
        private readonly IFileSelectionViewModelFactory fileSelectionViewModelFactory;
        private readonly ILogger<SecurityDescriptorTargetViewModel> logger;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IActiveDirectory directory;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly ILocalSam localSam;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly Func<IModelValidator<SecurityDescriptorTargetViewModel>> validator;
        private readonly ScriptTemplateProvider scriptTemplateProvider;
        private readonly IAmsLicenseManager licenseManager;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IViewModelFactory<SelectTargetTypeViewModel> selectTargetTypeFactory;
        private readonly IViewModelFactory<AzureAdObjectSelectorViewModel> aadObjectSelectorFactory;
        private readonly IAadGraphApiProvider graphProvider;
        private readonly IDeviceProvider deviceProvider;

        public SecurityDescriptorTargetViewModelFactory(IDialogCoordinator dialogCoordinator, IAppPathProvider appPathProvider, IViewModelFactory<NotificationChannelSelectionViewModel, AuditNotificationChannels> channelSelectionViewModelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, ILogger<SecurityDescriptorTargetViewModel> logger, IDiscoveryServices discoveryServices, IDomainTrustProvider domainTrustProvider, IActiveDirectory directory, ILocalSam localsam, IObjectSelectionProvider objectSelectionProvider, Func<IModelValidator<SecurityDescriptorTargetViewModel>> validator, ScriptTemplateProvider scriptTemplateProvider, IAmsLicenseManager licenseManager, IShellExecuteProvider shellExecuteProvider, IViewModelFactory<SelectTargetTypeViewModel> selectTargetTypeFactory, IViewModelFactory<AzureAdObjectSelectorViewModel> aadObjectSelectorFactory, IAadGraphApiProvider graphProvider, IDeviceProvider deviceProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.appPathProvider = appPathProvider;
            this.channelSelectionViewModelFactory = channelSelectionViewModelFactory;
            this.fileSelectionViewModelFactory = fileSelectionViewModelFactory;
            this.logger = logger;
            this.directory = directory;
            this.discoveryServices = discoveryServices;
            this.domainTrustProvider = domainTrustProvider;
            this.localSam = localsam;
            this.objectSelectionProvider = objectSelectionProvider;
            this.validator = validator;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.licenseManager = licenseManager;
            this.shellExecuteProvider = shellExecuteProvider;
            this.selectTargetTypeFactory = selectTargetTypeFactory;
            this.aadObjectSelectorFactory = aadObjectSelectorFactory;
            this.graphProvider = graphProvider;
            this.deviceProvider = deviceProvider;
        }

        public async Task<SecurityDescriptorTargetViewModel> CreateViewModelAsync(SecurityDescriptorTarget model, SecurityDescriptorTargetViewModelDisplaySettings settings)
        {

            var item = new SecurityDescriptorTargetViewModel(model, settings, channelSelectionViewModelFactory, fileSelectionViewModelFactory, appPathProvider, logger, dialogCoordinator, validator.Invoke(), directory, domainTrustProvider, discoveryServices, localSam, objectSelectionProvider, scriptTemplateProvider, licenseManager, shellExecuteProvider, selectTargetTypeFactory, aadObjectSelectorFactory, graphProvider, deviceProvider);

            await item.Initialization;

            return item;
        }
    }
}
