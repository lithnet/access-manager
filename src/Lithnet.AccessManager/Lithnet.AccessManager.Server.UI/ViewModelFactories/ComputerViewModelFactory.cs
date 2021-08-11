using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class ComputerViewModelFactory : IAsyncViewModelFactory<ComputerViewModel, IComputer>
    {
        private readonly ILogger<ComputerViewModel> logger;
        private readonly IAuthorityDataProvider authorityDataProvider;

        public ComputerViewModelFactory(ILogger<ComputerViewModel> logger, IAuthorityDataProvider authorityDataProvider)
        {
            this.logger = logger;
            this.authorityDataProvider = authorityDataProvider;
        }

        public async Task<ComputerViewModel> CreateViewModelAsync(IComputer model)
        {
            var vm = new ComputerViewModel(model, this.authorityDataProvider, this.logger);
            await vm.Initialization;
            return vm;
        }
    }
}
