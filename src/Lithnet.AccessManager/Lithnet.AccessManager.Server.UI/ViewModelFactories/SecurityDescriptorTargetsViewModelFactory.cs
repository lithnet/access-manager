using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModelFactory : ISecurityDescriptorTargetsViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly SecurityDescriptorTargetViewModelFactory factory;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IDiscoveryServices discoveryServices;
        private readonly ILogger<SecurityDescriptorTargetsViewModel> logger;

        public SecurityDescriptorTargetsViewModelFactory(IDialogCoordinator dialogCoordinator, SecurityDescriptorTargetViewModelFactory factory, INotifyModelChangedEventPublisher eventPublisher, IDiscoveryServices discoveryServices, ILogger<SecurityDescriptorTargetsViewModel> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.factory = factory;
            this.eventPublisher = eventPublisher;
            this.discoveryServices = discoveryServices;
            this.logger = logger;
        }

        public SecurityDescriptorTargetsViewModel CreateViewModel(IList<SecurityDescriptorTarget> model)
        {
            return new SecurityDescriptorTargetsViewModel(model, factory, dialogCoordinator, eventPublisher, discoveryServices, logger);
        }
    }
}
