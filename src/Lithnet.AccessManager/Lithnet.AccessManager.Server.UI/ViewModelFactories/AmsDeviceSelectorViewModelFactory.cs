using System;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDeviceSelectorViewModelFactory : IViewModelFactory<AmsDeviceSelectorViewModel>
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AmsDeviceSelectorViewModel> logger;
        private readonly IDeviceProvider deviceProvider;
        private readonly IModelValidator<AmsDeviceSelectorViewModel> validator;

        public AmsDeviceSelectorViewModelFactory(IDialogCoordinator dialogCoordinator, ILogger<AmsDeviceSelectorViewModel> logger, IAadGraphApiProvider graphProvider, IModelValidator<AmsDeviceSelectorViewModel> validator, IDeviceProvider deviceProvider)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.validator = validator;
            this.deviceProvider = deviceProvider;
        }

        public AmsDeviceSelectorViewModel CreateViewModel()
        {
            return new AmsDeviceSelectorViewModel(dialogCoordinator, logger, validator, deviceProvider);
        }
    }
}
