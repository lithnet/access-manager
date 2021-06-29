using System;
using Lithnet.AccessManager.Api;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class RegistrationKeyViewModelFactory : IRegistrationKeyViewModelFactory
    {
        private readonly Func<IModelValidator<RegistrationKeyViewModel>> validator;
        private readonly ILogger<RegistrationKeyViewModel> logger;

        public RegistrationKeyViewModelFactory(Func<IModelValidator<RegistrationKeyViewModel>> validator, ILogger<RegistrationKeyViewModel> logger)
        {
            this.validator = validator;
            this.logger = logger;
        }

        public RegistrationKeyViewModel CreateViewModel(IRegistrationKey model)
        {
            return new RegistrationKeyViewModel(model, this.logger, this.validator.Invoke());
        }
    }
}
