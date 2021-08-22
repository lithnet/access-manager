using Microsoft.Extensions.Logging;
using Stylet;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Lithnet.AccessManager.Server.UI
{
    public class ComputerSelectorViewModel : Screen, IExternalDialogAware
    {
        private readonly ILogger<ComputerSelectorViewModel> logger;
        private readonly IList<IComputer> computers;
        private readonly IAsyncViewModelFactory<ComputerViewModel, IComputer> computerViewModelFactory;

        public Task Initialization { get; }

        public ComputerSelectorViewModel(ILogger<ComputerSelectorViewModel> logger, IModelValidator<ComputerSelectorViewModel> validator, IList<IComputer> computers, IAsyncViewModelFactory<ComputerViewModel, IComputer> computerViewModelFactory) : base(validator)
        {
            this.logger = logger;
            this.computers = computers;
            this.computerViewModelFactory = computerViewModelFactory;
            this.Validate();
            this.SelectedItems = new ObservableCollection<ComputerViewModel>();
            this.Initialization = this.Initialize();
            this.DisplayName = "Select a computer";
        }

        private async Task Initialize()
        {
            List<ComputerViewModel> com = new List<ComputerViewModel>();
            foreach (var item in computers)
            {
                com.Add(await this.computerViewModelFactory.CreateViewModelAsync(item));
            }

            this.Devices = new BindableCollection<ComputerViewModel>(com);
            this.SelectedItem = this.Devices.FirstOrDefault();
        }

        public ObservableCollection<ComputerViewModel> SelectedItems { get; }

        public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

        public BindableCollection<ComputerViewModel> Devices { get; private set; }

        public ComputerViewModel SelectedItem { get; set; }

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = true;

        public string SaveButtonName { get; set; } = "Select...";

        public string CancelButtonName { get; set; } = "Cancel";
    }
}
