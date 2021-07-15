using System.Collections;
using System.Collections.Generic;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class EffectiveAccessViewModelFactory : IViewModelFactory<EffectiveAccessViewModel, SecurityDescriptorTargetsViewModel>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<EffectiveAccessViewModel> logger;
        private readonly IAuthorizationInformationBuilder authorizationBuilder;
        private readonly IActiveDirectory directory;
        private readonly IEnumerable<IComputerTargetProvider> computerTargetProvider;

        public EffectiveAccessViewModelFactory(IDialogCoordinator dialogCoordinator, ILogger<EffectiveAccessViewModel> logger, IAuthorizationInformationBuilder authorizationBuilder, IActiveDirectory directory, IEnumerable<IComputerTargetProvider> computerTargetProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.directory = directory;
            this.authorizationBuilder = authorizationBuilder;
            this.computerTargetProvider = computerTargetProvider;
        }

        public EffectiveAccessViewModel CreateViewModel(SecurityDescriptorTargetsViewModel targets)
        {
            return new EffectiveAccessViewModel(authorizationBuilder, dialogCoordinator, directory, targets, logger, computerTargetProvider);
        }
    }
}
