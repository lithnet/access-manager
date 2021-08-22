using System;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupSelectorViewModelFactory : IViewModelFactory<AmsGroupSelectorViewModel>
    {
        private readonly ILogger<AmsGroupSelectorViewModel> logger;
        private readonly IAmsGroupProvider groupProvider;
        private readonly Func<IModelValidator<AmsGroupSelectorViewModel>> validator;
        private readonly IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory;

        public AmsGroupSelectorViewModelFactory(ILogger<AmsGroupSelectorViewModel> logger, Func<IModelValidator<AmsGroupSelectorViewModel>> validator, IAmsGroupProvider groupProvider, IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory)
        {
            this.logger = logger;
            this.validator = validator;
            this.groupProvider = groupProvider;
            this.factory = factory;
        }

        public AmsGroupSelectorViewModel CreateViewModel()
        {
            return new AmsGroupSelectorViewModel(logger, validator.Invoke(), groupProvider, factory);
        }
    }
}
