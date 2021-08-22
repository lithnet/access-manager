using System;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDeviceSelectorViewModelFactory : IViewModelFactory<AmsDeviceSelectorViewModel>
    {
        private readonly ILogger<AmsDeviceSelectorViewModel> logger;
        private readonly IDeviceProvider deviceProvider;
        private readonly Func<IModelValidator<AmsDeviceSelectorViewModel>> validator;

        public AmsDeviceSelectorViewModelFactory(ILogger<AmsDeviceSelectorViewModel> logger, Func<IModelValidator<AmsDeviceSelectorViewModel>> validator, IDeviceProvider deviceProvider)
        {
            this.logger = logger;
            this.validator = validator;
            this.deviceProvider = deviceProvider;
        }

        public AmsDeviceSelectorViewModel CreateViewModel()
        {
            return new AmsDeviceSelectorViewModel(logger, validator.Invoke(), deviceProvider);
        }
    }
}
