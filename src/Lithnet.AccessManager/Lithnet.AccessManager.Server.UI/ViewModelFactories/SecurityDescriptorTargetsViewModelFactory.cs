using System.Collections.Generic;
using Lithnet.AccessManager.Server.Authorization;
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
        private readonly ILogger<SecurityDescriptorTargetsViewModel> logger;
        private readonly IDirectory directory;
        private readonly IComputerTargetProvider computerTargetProvider;
        private readonly IEffectiveAccessViewModelFactory effectiveAccessFactory;

        public SecurityDescriptorTargetsViewModelFactory(IDialogCoordinator dialogCoordinator, SecurityDescriptorTargetViewModelFactory factory, INotifyModelChangedEventPublisher eventPublisher, ILogger<SecurityDescriptorTargetsViewModel> logger, IDirectory directory, IComputerTargetProvider computerTargetProvider, IEffectiveAccessViewModelFactory effectiveAccessFactory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.factory = factory;
            this.eventPublisher = eventPublisher;
            this.logger = logger;
            this.directory = directory;
            this.computerTargetProvider = computerTargetProvider;
            this.effectiveAccessFactory = effectiveAccessFactory;
        }

        public SecurityDescriptorTargetsViewModel CreateViewModel(IList<SecurityDescriptorTarget> model)
        {
            return new SecurityDescriptorTargetsViewModel(model, factory, dialogCoordinator, eventPublisher, logger, directory, computerTargetProvider, effectiveAccessFactory);
        }
    }
}
