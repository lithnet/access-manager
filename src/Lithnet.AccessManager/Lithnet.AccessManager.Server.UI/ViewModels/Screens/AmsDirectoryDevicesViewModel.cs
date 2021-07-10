using System;
using System.Collections.Generic;
using MahApps.Metro.Controls.Dialogs;
using Stylet;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryDevicesViewModel : Screen, IHelpLink
    {
        private readonly IDeviceProvider deviceProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDevicePasswordProvider passwordProvider;
        private readonly ILogger<AmsDirectoryDevicesViewModel> logger;

        public AmsDirectoryDevicesViewModel(IDeviceProvider deviceProvider, IShellExecuteProvider shellExecuteProvider, IDialogCoordinator dialogCoordinator, IDevicePasswordProvider passwordProvider, ILogger<AmsDirectoryDevicesViewModel> logger)
        {
            this.deviceProvider = deviceProvider;
            this.shellExecuteProvider = shellExecuteProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.passwordProvider = passwordProvider;
            this.logger = logger;
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
            this.NotifyOfPropertyChange(() => this.CanEnableDevice);
            this.NotifyOfPropertyChange(() => this.CanDisableDevice);
            this.NotifyOfPropertyChange(() => this.CanExpirePassword);
        }


        protected override void OnInitialActivate()
        {
            Task.Run(async () => await this.Initialize());
        }

        private async Task Initialize()
        {
            try
            {
                this.IsLoading = true;

                List<DeviceViewModel> list = new List<DeviceViewModel>();
                await foreach (IDevice m in this.deviceProvider.GetDevices(0, 2000000))
                {
                    list.Add(new DeviceViewModel(m));
                }

                this.Devices = new BindableCollection<DeviceViewModel>(list);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }


        public bool IsLoading { get; set; }

        [NotifyModelChangedCollection]
        public BindableCollection<DeviceViewModel> Devices { get; set; }

        public DeviceViewModel SelectedItem { get; set; }

        public ObservableCollection<DeviceViewModel> SelectedItems { get; }

        public bool CanExpirePassword => this.SelectedItems.Count > 0;

        public async Task ExpirePassword()
        {
            try
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

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", $"Are you sure you want to expire the password on {message}?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
                {
                    foreach (var item in selectedItems)
                    {
                        await this.passwordProvider.ExpireCurrentPassword(item.ObjectID);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not expire the password\r\n{ex.Message}");
            }
        }

        public bool CanEnableDevice => this.SelectedItems.All(t => t.Disabled);

        public async Task EnableDevice()
        {
            try
            {
                var selectedItems = this.SelectedItems.Where(t => t.Disabled).ToList();

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

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", $"Are you sure you want to enable {message}?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
                {
                    foreach (var item in selectedItems)
                    {
                        await this.deviceProvider.EnableDevice(item.ObjectID);
                        item.Disabled = false;
                    }

                    this.NotifyOfPropertyChange(() => this.CanEnableDevice);
                    this.NotifyOfPropertyChange(() => this.CanDisableDevice);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not enable the device\r\n{ex.Message}");
            }
        }

        public bool CanDisableDevice => this.SelectedItems.All(t => t.Enabled);

        public async Task DisableDevice()
        {
            try
            {
                var selectedItems = this.SelectedItems.Where(t => t.Enabled).ToList();

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

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", $"Are you sure you want to disable {message}?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
                {
                    foreach (var item in selectedItems)
                    {
                        await this.deviceProvider.DisableDevice(item.ObjectID);
                        item.Disabled = true;
                    }

                    this.NotifyOfPropertyChange(() => this.CanEnableDevice);
                    this.NotifyOfPropertyChange(() => this.CanDisableDevice);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not disable the device\r\n{ex.Message}");
            }
        }

        public bool CanApproveDevice => this.SelectedItems.All(t => t.IsPending);

        public async Task ApproveDevice()
        {
            try
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
                        await this.deviceProvider.ApproveDevice(item.ObjectID);
                        item.ApprovalState = ApprovalState.Approved;
                    }

                    this.NotifyOfPropertyChange(() => this.CanRejectDevice);
                    this.NotifyOfPropertyChange(() => this.CanApproveDevice);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not approve the device\r\n{ex.Message}");
            }
        }

        public bool CanRejectDevice => this.SelectedItems.All(t => t.IsPending);

        public async Task RejectDevice()
        {
            try
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
                        await this.deviceProvider.RejectDevice(item.ObjectID);
                        item.ApprovalState = ApprovalState.Rejected;
                    }

                    this.NotifyOfPropertyChange(() => this.CanRejectDevice);
                    this.NotifyOfPropertyChange(() => this.CanApproveDevice);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not reject the device\r\n{ex.Message}");
            }
        }

        public bool CanDeleteDevice => !this.IsLoading && this.SelectedItems.Count > 0;

        public async Task Edit()
        {
            try
            {
                var selectedItem = this.SelectedItem;

                if (selectedItem == null)
                {
                    return;
                }

                DialogWindow w = new DialogWindow
                {
                    Title = "Device details",
                    SaveButtonIsDefault = true,
                    ShowCloseButton = false,
                    SaveButtonName = "Close",
                    DataContext = selectedItem
                };

                await this.GetWindow().ShowChildWindowAsync(w);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task DeleteDevice()
        {
            try
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
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public string HelpLink => Constants.HelpLinkPageJitAccess;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}