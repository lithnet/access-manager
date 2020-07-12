using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetsViewModelFactory : ISecurityDescriptorTargetsViewModelFactory
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly SecurityDescriptorTargetViewModelFactory factory;

        public SecurityDescriptorTargetsViewModelFactory(IDialogCoordinator dialogCoordinator, SecurityDescriptorTargetViewModelFactory factory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.factory = factory;
        }

        public SecurityDescriptorTargetsViewModel CreateViewModel(IList<SecurityDescriptorTarget> model)
        {
            return new SecurityDescriptorTargetsViewModel(model, factory, dialogCoordinator);
        }
    }
}
