using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDeviceSelectorViewModel : Screen
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AmsDeviceSelectorViewModel> logger;
        private readonly IDeviceProvider deviceProvider;

        public AmsDeviceSelectorViewModel(IDialogCoordinator dialogCoordinator, ILogger<AmsDeviceSelectorViewModel> logger, IModelValidator<AmsDeviceSelectorViewModel> validator, IDeviceProvider deviceProvider) : base(validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.deviceProvider = deviceProvider;
            this.Devices = new BindableCollection<DeviceViewModel>();
            this.Validate();
            this.SelectedItems = new ObservableCollection<DeviceViewModel>();
        }

        protected override void OnInitialActivate()
        {
            Task.Run(async () => await this.Initialize());
        }

        private async Task Initialize()
        {
            try
            {
                List<DeviceViewModel> list = new List<DeviceViewModel>();
                await foreach (IDevice m in this.deviceProvider.GetDevices())
                {
                    if (m.AuthorityType == AuthorityType.Ams)
                    {
                        list.Add(new DeviceViewModel(m));
                    }
                }

                this.Devices = new BindableCollection<DeviceViewModel>(list);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        public ObservableCollection<DeviceViewModel> SelectedItems { get; }

        public SelectionMode SelectionMode { get; set; } = SelectionMode.Single;

        public BindableCollection<DeviceViewModel> Devices { get; set; }

        public DeviceViewModel SelectedItem { get; set; }
    }
}
