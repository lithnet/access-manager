using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class MainWindowViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHandle<ModelChangedEvent>
    {
        private readonly IApplicationConfig model;
        private readonly IEventAggregator eventAggregator;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<MainWindowViewModel> logger;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IRegistryProvider registryProvider;
        private readonly ICertificateSynchronizationProvider certSyncProvider;
        private readonly IClusterProvider clusterProvider;
        private readonly HostingViewModel hosting;

        private SemaphoreSlim clusterWaitSemaphore;
        private int isUiLocked;

        public MainWindowViewModel(IApplicationConfig model, AuthorizationViewModel authorization, HostingViewModel hosting,
           HelpViewModel help, IEventAggregator eventAggregator, IDialogCoordinator dialogCoordinator, ILogger<MainWindowViewModel> logger, IShellExecuteProvider shellExecuteProvider, IWindowsServiceProvider windowsServiceProvider, IRegistryProvider registryProvider, ICertificateSynchronizationProvider certSyncProvider, IClusterProvider clusterProvider, ServerConfigurationViewModel serverConfigurationVm, DirectoryConfigurationViewModel directoryVm)
        {
            this.model = model;
            this.shellExecuteProvider = shellExecuteProvider;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.windowsServiceProvider = windowsServiceProvider;
            this.hosting = hosting;
            this.registryProvider = registryProvider;
            this.eventAggregator = eventAggregator;
            this.certSyncProvider = certSyncProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.clusterProvider = clusterProvider;
            this.clusterWaitSemaphore = new SemaphoreSlim(0, 1);

            this.eventAggregator.Subscribe(this);
            this.DisplayName = Constants.AppName;


            this.Items.Add(serverConfigurationVm);
            this.Items.Add(directoryVm);
            this.Items.Add(authorization);


            this.OptionItems = new BindableCollection<PropertyChangedBase> { help };

            this.ActiveItem = this.Items.First();

            this.UpdateIsConfigured();
            this.SetupClusterMonitor();

        }

        private object ai;

        public object Item
        {
            get => this.ai;
            set
            {
                this.ai = value;
                if (value is IScreenState s)
                {
                    s.Activate();
                }
            }
        }

        private void SetupClusterMonitor()
        {
            try
            {
                if (this.clusterProvider.IsClustered)
                {
                    this.clusterProvider.NodeChangedEvent += ClusterProvider_NodeChangedEventAsync;
                    this.clusterProvider.SetupClusterNodeChangeNotifications();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not set up cluster change notifications");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "Event handler")]
        private async void ClusterProvider_NodeChangedEventAsync(object sender, EventArgs e)
        {
            if (this.clusterProvider.IsOnActiveNode())
            {
                this.clusterWaitSemaphore.Release();
            }
            else
            {
                await this.DisableUIAndWaitForNode();
            }
        }

        public BindableCollection<PropertyChangedBase> OptionItems { get; }

        public PropertyChangedBase ActiveOptionsItem { get; set; }

        public string HelpLink => (this.ActiveItem as IHelpLink)?.HelpLink ?? (this.ActiveOptionsItem as IHelpLink)?.HelpLink;

        public async Task DisableUIAndWaitForNode()
        {
            ProgressDialogController controller = null;

            if (Interlocked.CompareExchange(ref isUiLocked, 1, 0) != 0)
            {
                return;
            }

            try
            {
                controller = await this.dialogCoordinator.ShowProgressAsync(this, "Cluster node no longer active", "This server is no longer hosting the clustered service. Configuration is currently disabled. You can wait for the service to return to this node, or close the app without saving any configuration changes. ", true, new MetroDialogSettings() { NegativeButtonText = "Close app without saving" });

                controller.Canceled += (sender, args) =>
                {
                    this.clusterWaitSemaphore.Release();
                    Application.Current.Shutdown();
                };

                await this.clusterWaitSemaphore.WaitAsync();
            }
            finally
            {
                isUiLocked = 0;

                if (controller != null)
                {
                    await controller.CloseAsync();
                }
            }
        }

        public async Task<bool> Save()
        {
            try
            {
                if (this.model.HasFileBeenModified())
                {
                    var result = await this.dialogCoordinator.ShowMessageAsync(this, "Warning", $"The configuration file has been modified outside of the editor. Do you want to overwrite the external changes with the current configuration, discard the changes made in this app and reload the config from the file, or cancel the save request? ", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Overwrite external changes",
                        NegativeButtonText = "Cancel save",
                        FirstAuxiliaryButtonText = "Reload config and discard changes",
                        DefaultButtonFocus = MessageDialogResult.Affirmative,
                    });

                    if (result == MessageDialogResult.Negative)
                    {
                        return false;
                    }
                    else if (result == MessageDialogResult.FirstAuxiliary)
                    {
                        System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                        Application.Current.Shutdown();
                        return false;
                    }
                }

                this.certSyncProvider.ExportCertificatesToConfig();
                this.model.Save(this.model.Path, true);
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error saving file", $"The configuration file could not be saved\r\n{ex.Message}");
                return false;
            }

            try
            {
                if (!await this.hosting.CommitSettings(this))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error saving service configuration", $"There was a problem updating the service configuration\r\n{ex.Message}");
                return false;
            }

            this.IsDirty = false;
            this.UpdateIsConfigured();

            if (this.IsRestartRequiredOnSave)
            {
                if (await this.dialogCoordinator.ShowMessageAsync(this, "Service configuration updated", $"The service must be restarted for the new settings to take effect. Do you want to restart the service now?", MessageDialogStyle.AffirmativeAndNegative,
                    new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Yes",
                        NegativeButtonText = "No"
                    }) == MessageDialogResult.Affirmative)
                {
                    await this.RestartService();
                    this.IsRestartRequiredOnSave = false;
                }
                else
                {
                    this.IsPendingServiceRestart = true;
                    try
                    {
                        await this.windowsServiceProvider.WaitForStatus(ServiceControllerStatus.Stopped);
                        await this.windowsServiceProvider.WaitForStatus(ServiceControllerStatus.Running);
                        this.IsPendingServiceRestart = false;
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(EventIDs.UIGenericError, ex, "Service status polling error");
                    }
                }
            }

            return true;
        }


        public void Close()
        {
            this.RequestClose();
        }

        public async Task Help()
        {
            if (this.HelpLink == null)
            {
                return;
            }

            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public void About()
        {
            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "About",
                SaveButtonIsDefault = false,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };


            var vm = new AboutViewModel();
            w.DataContext = vm;
            w.ShowDialog();
        }

        public override async Task<bool> CanCloseAsync()
        {
            if (!this.IsDirty)
            {
                return true;
            }

            var result = await this.dialogCoordinator.ShowMessageAsync(this, "Unsaved changed",
                    "Do you want to save your changes?",
                    MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, new MetroDialogSettings()
                    {
                        AffirmativeButtonText = "_Save",
                        NegativeButtonText = "_Cancel",
                        FirstAuxiliaryButtonText = "Do_n't Save",
                        DefaultButtonFocus = MessageDialogResult.Affirmative,
                        AnimateShow = false,
                        AnimateHide = false
                    });

            if (result == MessageDialogResult.Affirmative)
            {
                try
                {
                    return await this.Save();
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Unable to save the configuration");
                    await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to save the configuration\r\n{ex.Message}");
                }
            }
            else if (result == MessageDialogResult.FirstAuxiliary)
            {
                return true;
            }

            return false;
        }

        public string WindowTitle => $"{this.DisplayName}{(this.IsDirty ? "*" : "")}";

        public bool IsDirty { get; set; }

        public bool IsPendingServiceRestart { get; set; }

        public bool IsRestartRequiredOnSave { get; set; }

        public bool IsUnconfigured => !this.IsConfigured;

        public bool IsConfigured { get; set; }

        private void UpdateIsConfigured()
        {
            this.IsConfigured = this.registryProvider.IsConfigured;
        }

        public void Handle(ModelChangedEvent message)
        {
            this.IsDirty = true;

            if (message.RequiresServiceRestart)
            {
                this.IsRestartRequiredOnSave = true;
            }
        }

        public async Task RestartService()
        {
            ProgressDialogController progress = null;

            try
            {
                progress = await this.dialogCoordinator.ShowProgressAsync(this, "Restarting service", "Waiting for the service to stop", false);
                progress.SetIndeterminate();

                await this.windowsServiceProvider.StopServiceAsync();

                progress.SetMessage("Waiting for the service to start");

                await this.windowsServiceProvider.StartServiceAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not restart service");
            }
            finally
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress.CloseAsync();
                }
            }
        }
    }
}
