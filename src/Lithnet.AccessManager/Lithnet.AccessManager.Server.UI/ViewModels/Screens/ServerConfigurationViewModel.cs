using Lithnet.AccessManager.Enterprise;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class ServerConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        private CancellationTokenSource servicePollCts;

        private readonly IAmsLicenseManager licenseManager;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<ServerConfigurationViewModel> logger;
        private readonly IApplicationUpgradeProvider appUpgradeProvider;

        public ServerConfigurationViewModel(HostingViewModel hostingVm, LicensingViewModel licensingVm, AuthenticationViewModel authenticationVm, EmailViewModel emailVm, RateLimitsViewModel rateLimitsVm, IpDetectionViewModel ipDetectionVm, AuditingViewModel auditingVm, HighAvailabilityViewModel haVm, DatabaseViewModel dbVm, IAmsLicenseManager licenseManager, IWindowsServiceProvider windowsServiceProvider, IShellExecuteProvider shellExecuteProvider, IDialogCoordinator dialogCoordinator, ILogger<ServerConfigurationViewModel> logger, IApplicationUpgradeProvider appUpgradeProvider)
        {
            this.licenseManager = licenseManager;
            this.windowsServiceProvider = windowsServiceProvider;
            this.shellExecuteProvider = shellExecuteProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.appUpgradeProvider = appUpgradeProvider;

            this.Items.Add(hostingVm);
            this.Items.Add(licensingVm);
            this.Items.Add(authenticationVm);
            this.Items.Add(emailVm);
            this.Items.Add(rateLimitsVm);
            this.Items.Add(ipDetectionVm);
            this.Items.Add(auditingVm);
            this.Items.Add(haVm);
            this.Items.Add(dbVm);

            this.DisplayName = "Server configuration";
            this.ServiceStatus = this.windowsServiceProvider.Status.ToString();
            this.HostingViewModel = hostingVm;

            this.licenseManager.OnLicenseDataChanged += delegate
            {
                this.NotifyOfPropertyChange(nameof(this.IsEnterpriseEdition));
                this.NotifyOfPropertyChange(nameof(this.IsStandardEdition));
            };
        }

        public HostingViewModel HostingViewModel { get; }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        public PackIconFeatherIconsKind Icon => PackIconFeatherIconsKind.Server;

        public bool IsEnterpriseEdition => this.licenseManager.IsEnterpriseEdition();

        public bool IsStandardEdition => !this.IsEnterpriseEdition;

        public string AvailableVersion { get; set; }

        public bool CanStartService => this.ServiceStatus == ServiceControllerStatus.Stopped.ToString();

        public bool CanStopService => this.ServiceStatus == ServiceControllerStatus.Running.ToString();

        public string CurrentVersion { get; set; }

        public bool IsUpToDate { get; set; }

        public bool ServicePending { get; set; }

        public string ServiceStatus { get; set; }

        public bool UpdateAvailable { get; set; }

        public string UpdateLink { get; set; }

        public async Task DownloadUpdate()
        {
            if (this.UpdateLink == null)
            {
                return;
            }

            await this.shellExecuteProvider.OpenWithShellExecute(this.UpdateLink);
        }

        public async Task StartService()
        {
            ProgressDialogController progress = null;

            try
            {
                if (this.CanStartService)
                {
                    progress = await this.dialogCoordinator.ShowProgressAsync(this, "Starting service", "Waiting for service to start", false, new MetroDialogSettings { AnimateHide = false, AnimateShow = false });
                    progress.SetIndeterminate();
                    await Task.Delay(500);

                    await this.windowsServiceProvider.StartServiceAsync();
                }
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress?.CloseAsync();
                }

                await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not start in the requested time");
            }
            catch (Exception ex)
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress?.CloseAsync();
                }

                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not start service");
                await dialogCoordinator.ShowMessageAsync(this, "Service control", $"Could not start service\r\n{ex.Message}");
            }
            finally
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress?.CloseAsync();
                }
            }
        }

        public async Task StopService()
        {
            ProgressDialogController progress = null;

            try
            {
                if (this.CanStopService)
                {
                    progress = await this.dialogCoordinator.ShowProgressAsync(this, "Stopping service", "Waiting for service to stop", false, new MetroDialogSettings { AnimateHide = false, AnimateShow = false });
                    progress.SetIndeterminate();
                    await Task.Delay(500);

                    await this.windowsServiceProvider.StopServiceAsync();
                }
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress?.CloseAsync();
                }

                await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not stop in the requested time");
            }
            catch (Exception ex)
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress?.CloseAsync();
                }

                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not stop service");
                await dialogCoordinator.ShowMessageAsync(this, "Service control", $"Could not stop service\r\n{ex.Message}");
            }
            finally
            {
                if (progress?.IsOpen ?? false)
                {
                    await progress?.CloseAsync();
                }
            }
        }

        public async Task TryGetVersion()
        {
            this.UpdateAvailable = false;
            this.IsUpToDate = false;
            this.UpdateLink = null;
            this.AvailableVersion = null;

            try
            {
                var versionInfo = await this.appUpgradeProvider.GetVersionInfo();
                this.CurrentVersion = versionInfo.CurrentVersion?.ToString() ?? "Could not determine version";

                if (versionInfo.Status == VersionInfoStatus.Failed || versionInfo.Status == VersionInfoStatus.Unknown)
                {
                    this.AvailableVersion = "Unable to determine latest application version";
                }

                this.AvailableVersion = versionInfo.AvailableVersion?.ToString();
                this.UpdateLink = versionInfo.UpdateUrl;
                this.IsUpToDate = versionInfo.Status == VersionInfoStatus.Current;
                this.UpdateAvailable = versionInfo.Status == VersionInfoStatus.UpdateAvailable;
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not get version update");
                this.AvailableVersion = "Unable to determine latest application version";
            }
        }

        protected override void OnInitialActivate()
        {
            _ = this.TryGetVersion();
        }

        protected override void OnActivate()
        {
            this.servicePollCts = new CancellationTokenSource();
            _ = this.PollServiceStatus(this.servicePollCts.Token);
            base.OnActivate();
        }

        protected override void OnDeactivate()
        {
            this.servicePollCts.Cancel();
            base.OnDeactivate();
        }

        private async Task PollServiceStatus(CancellationToken token)
        {
            try
            {
                Debug.WriteLine("Poll started");
                ServiceControllerStatus lastStatus = 0;

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);

                    if (lastStatus == this.windowsServiceProvider.Status)
                    {
                        continue;
                    }

                    ServiceControllerStatus currentStatus = this.windowsServiceProvider.Status;

                    switch (currentStatus)
                    {
                        case ServiceControllerStatus.StartPending:
                            this.ServiceStatus = "Starting";
                            break;

                        case ServiceControllerStatus.StopPending:
                            this.ServiceStatus = "Stopping";
                            break;

                        case ServiceControllerStatus.ContinuePending:
                            this.ServiceStatus = "Continue pending";

                            break;

                        case ServiceControllerStatus.PausePending:
                            this.ServiceStatus = "Pausing";
                            break;

                        default:
                            this.ServiceStatus = currentStatus.ToString();
                            break;
                    }

                    this.ServicePending = currentStatus == ServiceControllerStatus.ContinuePending ||
                                          currentStatus == ServiceControllerStatus.PausePending ||
                                          currentStatus == ServiceControllerStatus.StartPending ||
                                          currentStatus == ServiceControllerStatus.StopPending;

                    lastStatus = currentStatus;
                }
            }
            catch
            {
                this.ServicePending = false;
                this.ServiceStatus = "Unknown";
            }

            Debug.WriteLine("Poll stopped");
        }
    }
}
