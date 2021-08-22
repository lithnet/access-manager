using System;
using Microsoft.Extensions.Logging;
using Stylet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class ComputerSelectorViewModelFactory : IAsyncViewModelFactory<ComputerSelectorViewModel, IList<IComputer>>
    {
        private readonly ILogger<ComputerSelectorViewModel> logger;
        private readonly Func<IModelValidator<ComputerSelectorViewModel>> validator;
        private readonly IAsyncViewModelFactory<ComputerViewModel, IComputer> computerViewModelFactory;

        public ComputerSelectorViewModelFactory(ILogger<ComputerSelectorViewModel> logger, Func<IModelValidator<ComputerSelectorViewModel>> validator, IAsyncViewModelFactory<ComputerViewModel, IComputer> computerViewModelFactory)
        {
            this.logger = logger;
            this.validator = validator;
            this.computerViewModelFactory = computerViewModelFactory;
        }

        public async Task<ComputerSelectorViewModel> CreateViewModelAsync(IList<IComputer> computers)
        {
            var vm = new ComputerSelectorViewModel(logger, validator.Invoke(), computers, computerViewModelFactory);
            await vm.Initialization;
            return vm;
        }
    }
}
