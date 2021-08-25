using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDeviceSelectorViewModel : Screen, IExternalDialogAware, IHasSize
    {
        private readonly ILogger<AmsDeviceSelectorViewModel> logger;
        private readonly IDeviceProvider deviceProvider;

        public AmsDeviceSelectorViewModel(ILogger<AmsDeviceSelectorViewModel> logger, IModelValidator<AmsDeviceSelectorViewModel> validator, IDeviceProvider deviceProvider) : base(validator)
        {
            this.logger = logger;
            this.deviceProvider = deviceProvider;
            this.Devices = new BindableCollection<DeviceViewModel>();
            this.Validate();
            this.SelectedItems = new ObservableCollection<DeviceViewModel>();
            this.DisplayName = "Select an AMS device";
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
                this.SelectedItem = this.Devices.FirstOrDefault();
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

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = true;

        public string SaveButtonName { get; set; } = "Select...";

        public string CancelButtonName { get; set; } = "Cancel";

        public int Width { get; } = 800;

        public int Height { get; } = 500;
    }
}
