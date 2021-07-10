using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModelFactory : IAsyncViewModelFactory<SecurityDescriptorTargetsViewModel, IList<SecurityDescriptorTarget>>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAsyncViewModelFactory<SecurityDescriptorTargetViewModel, SecurityDescriptorTarget, SecurityDescriptorTargetViewModelDisplaySettings> factory;
        private readonly Func<INotifyModelChangedEventPublisher> eventPublisher;
        private readonly ILogger<SecurityDescriptorTargetsViewModel> logger;
        private readonly IDirectory directory;
        private readonly IEnumerable<IComputerTargetProvider> computerTargetProviders;
        private readonly IViewModelFactory<EffectiveAccessViewModel, SecurityDescriptorTargetsViewModel> effectiveAccessFactory;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public SecurityDescriptorTargetsViewModelFactory(IDialogCoordinator dialogCoordinator, IAsyncViewModelFactory<SecurityDescriptorTargetViewModel, SecurityDescriptorTarget, SecurityDescriptorTargetViewModelDisplaySettings> factory, Func<INotifyModelChangedEventPublisher> eventPublisher, ILogger<SecurityDescriptorTargetsViewModel> logger, IDirectory directory, IEnumerable<IComputerTargetProvider> computerTargetProviders, IViewModelFactory<EffectiveAccessViewModel, SecurityDescriptorTargetsViewModel> effectiveAccessFactory, IShellExecuteProvider shellExecuteProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.factory = factory;
            this.eventPublisher = eventPublisher;
            this.logger = logger;
            this.directory = directory;
            this.computerTargetProviders = computerTargetProviders;
            this.effectiveAccessFactory = effectiveAccessFactory;
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public async Task<SecurityDescriptorTargetsViewModel> CreateViewModelAsync(IList<SecurityDescriptorTarget> model)
        {
            var item = new SecurityDescriptorTargetsViewModel(model, factory, dialogCoordinator, eventPublisher.Invoke(), logger, directory, computerTargetProviders, effectiveAccessFactory, shellExecuteProvider);
            await item.Initialization;
            return item;
        }
    }
}
