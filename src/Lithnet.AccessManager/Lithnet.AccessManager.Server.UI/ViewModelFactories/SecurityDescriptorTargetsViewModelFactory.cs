using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModelFactory : ISecurityDescriptorTargetsViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ISecurityDescriptorTargetViewModelFactory factory;
        private readonly Func<INotifyModelChangedEventPublisher> eventPublisher;
        private readonly ILogger<SecurityDescriptorTargetsViewModel> logger;
        private readonly IDirectory directory;
        private readonly IComputerTargetProvider computerTargetProvider;
        private readonly IEffectiveAccessViewModelFactory effectiveAccessFactory;

        public SecurityDescriptorTargetsViewModelFactory(IDialogCoordinator dialogCoordinator, ISecurityDescriptorTargetViewModelFactory factory, Func<INotifyModelChangedEventPublisher> eventPublisher, ILogger<SecurityDescriptorTargetsViewModel> logger, IDirectory directory, IComputerTargetProvider computerTargetProvider, IEffectiveAccessViewModelFactory effectiveAccessFactory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.factory = factory;
            this.eventPublisher = eventPublisher;
            this.logger = logger;
            this.directory = directory;
            this.computerTargetProvider = computerTargetProvider;
            this.effectiveAccessFactory = effectiveAccessFactory;
        }

        public async Task<SecurityDescriptorTargetsViewModel> CreateViewModelAsync(IList<SecurityDescriptorTarget> model)
        {
            var item = new SecurityDescriptorTargetsViewModel(model, factory, dialogCoordinator, eventPublisher.Invoke(), logger, directory, computerTargetProvider, effectiveAccessFactory);
            await item.Initialization;
            return item;
        }
    }
}
