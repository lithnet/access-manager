using Lithnet.AccessManager.Enterprise;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryDevicesViewModel : Screen, IHelpLink
    {
        private readonly IDeviceProvider deviceProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDialogCoordinator dialogCoordinator;

        public AmsDirectoryDevicesViewModel(IDeviceProvider deviceProvider, IShellExecuteProvider shellExecuteProvider, IDialogCoordinator dialogCoordinator)
        {
            this.deviceProvider = deviceProvider;
            this.shellExecuteProvider = shellExecuteProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.DisplayName = "Devices";
            this.Devices = new BindableCollection<DeviceViewModel>();
            this.SelectedItems = new ObservableCollection<DeviceViewModel>();
            this.SelectedItems.CollectionChanged += this.SelectedItems_CollectionChanged;
        }

        private void SelectedItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.NotifyOfPropertyChange(() => this.CanRejectDevice);
            this.NotifyOfPropertyChange(() => this.CanApproveDevice);
            this.NotifyOfPropertyChange(() => this.CanDeleteDevice);
        }

        
        protected override void OnInitialActivate()
        {
            Task.Run(async () =>
            {
                this.IsLoading = true;

                await foreach (var m in this.deviceProvider.GetDevices(0, 200000))
                {
                    this.Devices.Add(new DeviceViewModel(m));
                }

                this.IsLoading = false;
            });
        }

        public bool IsLoading { get; set; }

        [NotifyModelChangedCollection]
        public BindableCollection<DeviceViewModel> Devices { get; }

        public DeviceViewModel SelectedItem { get; set; }

        public ObservableCollection<DeviceViewModel> SelectedItems { get; }

        public bool CanApproveDevice => this.SelectedItems.All(t => t.IsPending);

        public async Task ApproveDevice()
        {
            var selectedItems = this.SelectedItems.Where(t => t.IsPending).ToList();

            if (selectedItems.Count == 0)
            {
                return;
            }

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            string message = "this device";

            if (selectedItems.Count > 1)
            {
                message = $"these {selectedItems.Count} devices";
            }

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", $"Are you sure you want to approve {message}?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                foreach (var item in selectedItems)
                {
                    if (item.IsPending)
                    {
                        await this.deviceProvider.ApproveDevice(item.ObjectID);
                        item.ApprovalState = ApprovalState.Approved;
                    }
                }

                this.NotifyOfPropertyChange(() => this.CanRejectDevice);
                this.NotifyOfPropertyChange(() => this.CanApproveDevice);
            }
        }

        public bool CanRejectDevice => this.SelectedItems.All(t => t.IsPending);

        public async Task RejectDevice()
        {
            var selectedItems = this.SelectedItems.Where(t => t.IsPending).ToList();

            if (selectedItems.Count == 0)
            {
                return;
            }

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            string message = "this device";

            if (selectedItems.Count > 1)
            {
                message = $"these {selectedItems.Count} devices";
            }

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", $"Are you sure you want to reject {message}?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                foreach (var item in selectedItems)
                {
                    if (item.IsPending)
                    {
                        await this.deviceProvider.RejectDevice(item.ObjectID);
                        item.ApprovalState = ApprovalState.Rejected;
                    }
                }

                this.NotifyOfPropertyChange(() => this.CanRejectDevice);
                this.NotifyOfPropertyChange(() => this.CanApproveDevice);
            }
        }

        public bool CanDeleteDevice => !this.IsLoading && this.SelectedItems.Count > 0;

        public async Task ViewDevice()
        {

        }

        public async Task DeleteDevice()
        {
            var selectedItems = this.SelectedItems.ToList();

            if (selectedItems.Count == 0)
            {
                return;
            }

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            string message = "this device";

            if (selectedItems.Count > 1)
            {
                message = $"these {selectedItems.Count} devices";
            }

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Warning", $"Are you sure you want to delete {message}? This will remove ALL stored data, including passwords and device credentials. This operation can not be undone", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                foreach (var item in selectedItems)
                {
                    await this.deviceProvider.DeleteDevice(item.ObjectID);
                    this.Devices.Remove(item);
                }

                this.NotifyOfPropertyChange(() => this.CanRejectDevice);
                this.NotifyOfPropertyChange(() => this.CanApproveDevice);
                this.NotifyOfPropertyChange(() => this.CanDeleteDevice);
            }
        }

        public string HelpLink => Constants.HelpLinkPageJitAccess;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}