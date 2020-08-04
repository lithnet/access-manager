using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModelFactory : ISecurityDescriptorTargetsViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly SecurityDescriptorTargetViewModelFactory factory;
        private readonly INotifiableEventPublisher eventPublisher;

        public SecurityDescriptorTargetsViewModelFactory(IDialogCoordinator dialogCoordinator, SecurityDescriptorTargetViewModelFactory factory, INotifiableEventPublisher eventPublisher)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.factory = factory;
            this.eventPublisher = eventPublisher;
        }

        public SecurityDescriptorTargetsViewModel CreateViewModel(IList<SecurityDescriptorTarget> model)
        {
            return new SecurityDescriptorTargetsViewModel(model, factory, dialogCoordinator, eventPublisher);
        }
    }
}
