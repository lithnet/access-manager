using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class HostingViewModel : Screen, IHelpLink
    {
        private CancellationTokenSource servicePollCts;

        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<HostingViewModel> logger;
        private readonly IAppPathProvider pathProvider;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly ICertificateProvider certProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IDirectory directory;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ICertificatePermissionProvider certPermissionProvider;
        private readonly IRegistryProvider registryProvider;
        private readonly ISecretRekeyProvider rekeyProvider;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly IDiscoveryServices discoveryServices;
        private readonly ILicenseManager licenseManager;
        private readonly IApplicationUpgradeProvider appUpgradeProvider;
        private readonly IHttpSysConfigurationProvider certificateBindingProvider;
        private readonly IFirewallProvider firewallProvider;

        public HostingViewModel(HostingOptions model, IDialogCoordinator dialogCoordinator, IWindowsServiceProvider windowsServiceProvider, ILogger<HostingViewModel> logger, IModelValidator<HostingViewModel> validator, IAppPathProvider pathProvider, INotifyModelChangedEventPublisher eventPublisher, ICertificateProvider certProvider, IShellExecuteProvider shellExecuteProvider, IEventAggregator eventAggregator, IDirectory directory, IScriptTemplateProvider scriptTemplateProvider, ICertificatePermissionProvider certPermissionProvider, IRegistryProvider registryProvider, ISecretRekeyProvider rekeyProvider, IObjectSelectionProvider objectSelectionProvider, IDiscoveryServices discoveryServices, ILicenseManager licenseManager, IApplicationUpgradeProvider appUpgradeProvider, IHttpSysConfigurationProvider certificateBindingProvider, IFirewallProvider firewallProvider)
        {
            this.logger = logger;
            this.pathProvider = pathProvider;
            this.OriginalModel = model;
            this.certProvider = certProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.windowsServiceProvider = windowsServiceProvider;
            this.shellExecuteProvider = shellExecuteProvider;
            this.eventAggregator = eventAggregator;
            this.Validator = validator;
            this.directory = directory;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.certPermissionProvider = certPermissionProvider;
            this.registryProvider = registryProvider;
            this.rekeyProvider = rekeyProvider;
            this.objectSelectionProvider = objectSelectionProvider;
            this.discoveryServices = discoveryServices;
            this.licenseManager = licenseManager;
            this.appUpgradeProvider = appUpgradeProvider;
            this.certificateBindingProvider = certificateBindingProvider;
            this.firewallProvider = firewallProvider;

            this.WorkingModel = this.CloneModel(model);
            this.Certificate = this.certificateBindingProvider.GetCertificate();
            this.OriginalCertificate = this.Certificate;
            this.ServiceAccount = this.windowsServiceProvider.GetServiceAccountSid();
            this.ServiceStatus = this.windowsServiceProvider.Status.ToString();
            this.DisplayName = "Web hosting";

            this.licenseManager.OnLicenseDataChanged += delegate
            {
                this.NotifyOfPropertyChange(nameof(this.IsEnterpriseEdition));
                this.NotifyOfPropertyChange(nameof(this.IsStandardEdition));
            };

            eventPublisher.Register(this);
        }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        protected override void OnInitialActivate()
        {
            _ = this.TryGetVersion();
            this.PopulateCanDelegate();
            this.PopulateIsNotGmsa();
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

        public bool IsEnterpriseEdition => this.licenseManager.IsEnterpriseEdition();

        public bool IsStandardEdition => !this.IsEnterpriseEdition;

        public string AvailableVersion { get; set; }

        public bool CanShowCertificateDialog => this.Certificate != null;

        public bool CanStartService => this.ServiceStatus == ServiceControllerStatus.Stopped.ToString();

        public bool CanStopService => this.ServiceStatus == ServiceControllerStatus.Running.ToString();

        [NotifyModelChangedProperty]
        public X509Certificate2 Certificate { get; set; }

        public string CertificateDisplayName => this.Certificate.ToDisplayName();

        public string CertificateExpiryText { get; set; }

        public string CurrentVersion { get; set; }

        [NotifyModelChangedProperty]
        public string Hostname { get => this.WorkingModel.HttpSys.Hostname; set => this.WorkingModel.HttpSys.Hostname = value; }

        [NotifyModelChangedProperty]
        public int HttpPort { get => this.WorkingModel.HttpSys.HttpPort; set => this.WorkingModel.HttpSys.HttpPort = value; }

        [NotifyModelChangedProperty]
        public int HttpsPort { get => this.WorkingModel.HttpSys.HttpsPort; set => this.WorkingModel.HttpSys.HttpsPort = value; }

        public PackIconMaterialKind Icon => PackIconMaterialKind.Web;

        public bool IsCertificateCurrent { get; set; }

        public bool IsCertificateExpired { get; set; }

        public bool IsCertificateExpiring { get; set; }

        public bool IsUpToDate { get; set; }

        public bool IsConfigured { get; set; }

        public bool IsUnconfigured => !this.IsConfigured;

        [NotifyModelChangedProperty]
        public SecurityIdentifier ServiceAccount { get; set; }

        public string ServiceAccountDisplayName
        {
            get
            {
                try
                {
                    return this.ServiceAccount?.Translate(typeof(NTAccount))?.Value ?? "<not set>";
                }
                catch
                {
                    return this.ServiceAccount?.ToString() ?? "<not set>";
                }
            }
        }

        public bool ServicePending { get; set; }

        public string ServiceStatus { get; set; }

        public bool CanBeDelegated { get; set; }

        public bool IsNotGmsa { get; set; }

        private void PopulateCanDelegate()
        {
            try
            {
                this.CanBeDelegated = false;
                this.CanBeDelegated = this.directory.CanAccountBeDelegated(this.ServiceAccount);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not determine delegation status of account");
            }
        }

        private void PopulateIsNotGmsa()
        {
            try
            {
                this.IsNotGmsa = false;
                this.IsNotGmsa = !this.directory.IsAccountGmsa(this.ServiceAccount);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not determine GMSA status of account");
            }
        }

        public async Task OpenDelegationWarningHelpLink()
        {
            await shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkPageHostingDelegationWarning);
        }

        public async Task OpenGmsaInfo()
        {
            await shellExecuteProvider.OpenWithShellExecute(Constants.LinkGmsaInfo);
        }

        public void PreventDelegation()
        {
            ScriptContentViewModel vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run the following script as an account that is a member of the 'Domain admins' group",
                ScriptText = this.scriptTemplateProvider.PreventDelegation
                    .Replace("{sid}", this.ServiceAccount.ToString(), StringComparison.OrdinalIgnoreCase)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "Script",
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();

            this.PopulateCanDelegate();
        }

        public void CreateGmsa()
        {
            ScriptContentViewModel vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run the following script as an account that is a member of the 'Domain admins' group",
                ScriptText = this.scriptTemplateProvider.CreateGmsa
                    .Replace("{serverName}", Environment.MachineName, StringComparison.OrdinalIgnoreCase)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "Script",
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();
        }

        public bool ShowCertificateExpiryWarning => this.Certificate != null && this.Certificate.NotAfter.AddDays(-30) >= DateTime.Now;

        public bool UpdateAvailable { get; set; }

        public string UpdateLink { get; set; }

        private X509Certificate2 OriginalCertificate { get; set; }

        private HostingOptions OriginalModel { get; set; }

        private HostingOptions WorkingModel { get; set; }

        private string workingServiceAccountPassword { get; set; }

        private string workingServiceAccountUserName { get; set; }

        private void UpdateIsConfigured()
        {
            this.IsConfigured = registryProvider.IsConfigured;
        }

        public async Task<bool> CommitSettings(object dialogContext = null)
        {
            dialogContext ??= this;

            if (this.Certificate == null)
            {
                await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", "You must select a HTTPS certificate");
                return false;
            }

            this.UpdateIsConfigured();

            bool currentlyUnconfigured = this.IsUnconfigured;

            bool updateHttpReservations =
                this.WorkingModel.HttpSys.Hostname != this.OriginalModel.HttpSys.Hostname ||
                this.WorkingModel.HttpSys.HttpPort != this.OriginalModel.HttpSys.HttpPort ||
                this.WorkingModel.HttpSys.HttpsPort != this.OriginalModel.HttpSys.HttpsPort ||
                currentlyUnconfigured;

            bool updateConfigFile = updateHttpReservations;

            bool updateFirewallRules = updateHttpReservations;

            bool updateCertificateBinding =
                this.WorkingModel.HttpSys.HttpsPort != this.OriginalModel.HttpSys.HttpsPort ||
                this.Certificate?.Thumbprint != this.OriginalCertificate?.Thumbprint ||
                currentlyUnconfigured;

            bool updateServiceAccount = this.workingServiceAccountUserName != null;

            HostingSettingsRollbackContext rollbackContext = new HostingSettingsRollbackContext();
            rollbackContext.StartingUnconfigured = currentlyUnconfigured;

            try
            {
                if (updateHttpReservations)
                {
                    string httpOld = this.OriginalModel.HttpSys.BuildHttpUrlPrefix();
                    string httpsOld = this.OriginalModel.HttpSys.BuildHttpsUrlPrefix();
                    string httpNew = this.WorkingModel.HttpSys.BuildHttpUrlPrefix();
                    string httpsNew = this.WorkingModel.HttpSys.BuildHttpsUrlPrefix();

                    if (this.IsReservationInUse(currentlyUnconfigured, httpOld, httpNew, out string user))
                    {
                        MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Warning", $"The HTTP URL '{this.WorkingModel.HttpSys.BuildHttpUrlPrefix()}' is already registered to user {user}. Do you want to overwrite it?", MessageDialogStyle.AffirmativeAndNegative);

                        if (result == MessageDialogResult.Negative)
                        {
                            return false;
                        }
                    }

                    if (this.IsReservationInUse(currentlyUnconfigured, httpsOld, httpsNew, out user))
                    {
                        MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Warning", $"The HTTPS URL '{this.WorkingModel.HttpSys.BuildHttpsUrlPrefix()}' is already registered to user {user}. Do you want to overwrite it?", MessageDialogStyle.AffirmativeAndNegative);

                        if (result == MessageDialogResult.Negative)
                        {
                            return false;
                        }
                    }

                    this.certificateBindingProvider.CreateNewHttpReservations(this.OriginalModel.HttpSys, this.WorkingModel.HttpSys, rollbackContext.RollbackActions);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Error creating HTTP reservations");
                rollbackContext.Rollback(this.logger);
                await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", $"Could not create the HTTP reservations\r\n{ex.Message}");
                return false;
            }

            try
            {
                if (updateFirewallRules)
                {
                    this.firewallProvider.ReplaceFirewallRules(this.HttpPort, this.HttpsPort, rollbackContext.RollbackActions);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Error updating the firewall rules");
                rollbackContext.Rollback(this.logger);

                await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", $"Could not update the firewall rules. Please manually update them to ensure your users can access the application\r\n{ex.Message}");
                return false;
            }

            try
            {
                if (updateConfigFile)
                {
                    this.SaveHostingConfigFile(rollbackContext);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Could not save updated config file");
                rollbackContext.Rollback(this.logger);
                return false;
            }

            try
            {
                if (updateCertificateBinding)
                {
                    this.certificateBindingProvider.UpdateCertificateBinding(this.Certificate.Thumbprint, this.WorkingModel.HttpSys.HttpsPort, rollbackContext.RollbackActions);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Error creating certificate binding");
                rollbackContext.Rollback(this.logger);
                await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", $"Could not bind the certificate to the specified port\r\n{ex.Message}");

                return false;
            }

            try
            {
                if (updateServiceAccount)
                {
                    this.UpdateServiceAccount();

                    if (!await this.rekeyProvider.TryReKeySecretsAsync(this))
                    {
                        await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", $"You'll need to re-enter the secrets before you can save the file");
                        return false;
                    }

                    this.workingServiceAccountUserName = null;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex,
                    "Could not change the service account to the specified account {serviceAccountName}",
                    workingServiceAccountUserName);
                rollbackContext.Rollback(this.logger);
                await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", $"The service account could not be changed\r\n{ex.Message}");
                return false;
            }

            if (currentlyUnconfigured)
            {
                this.registryProvider.IsConfigured = true;
                this.UpdateIsConfigured();
            }

            if (updateCertificateBinding || updateHttpReservations || updateServiceAccount || updateConfigFile || updateFirewallRules)
            {
                this.OriginalModel = this.CloneModel(this.WorkingModel);
                this.OriginalCertificate = this.Certificate;
                this.eventAggregator.Publish(new ModelChangedEvent(this, "Hosting", true));
            }

            return true;
        }

        private void UpdateServiceAccount()
        {
            this.windowsServiceProvider.SetServiceAccount(this.workingServiceAccountUserName, this.workingServiceAccountPassword);
        }

        private void SaveHostingConfigFile(HostingSettingsRollbackContext rollback)
        {
            HostingOptions originalSettings = this.OriginalModel;

            this.WorkingModel.Save(pathProvider.HostingConfigFile);
            rollback.RollbackActions.Add(() => originalSettings.Save(pathProvider.HostingConfigFile));
        }

        public async Task DownloadUpdate()
        {
            if (this.UpdateLink == null)
            {
                return;
            }

            await this.shellExecuteProvider.OpenWithShellExecute(this.UpdateLink);
        }

        public void OnCertificateChanged()
        {
            this.IsCertificateCurrent = false;
            this.IsCertificateExpired = false;
            this.IsCertificateExpiring = false;

            if (this.Certificate == null)
            {
                this.CertificateExpiryText = "Select a certificate";
                return;
            }

            TimeSpan remainingTime = this.Certificate.NotAfter.Subtract(DateTime.Now);

            if (remainingTime.Ticks <= 0)
            {
                this.IsCertificateExpired = true;
                this.CertificateExpiryText = "The certificate has expired";
                return;
            }

            if (remainingTime.TotalDays < 30)
            {
                this.IsCertificateExpiring = true;
            }
            else
            {
                this.IsCertificateCurrent = true;
            }

            this.CertificateExpiryText = $"Certificate expires in {remainingTime:%d} days";
        }

        public async Task SelectServiceAccountUser()
        {
            try
            {
                if (!this.objectSelectionProvider.GetUserOrServiceAccount(this, out SecurityIdentifier sid))
                {
                    return;
                }

                if (!directory.TryGetPrincipal(sid, out ISecurityPrincipal o))
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not find user in the directory");
                    return;
                }

                string password = null;
                bool logonAsServiceFailed = false;

                try
                {
                    this.windowsServiceProvider.GrantLogonAsAService(o.MsDsPrincipalName);
                }
                catch (Exception ex)
                {
                    logonAsServiceFailed = true;
                    this.logger.LogError(EventIDs.UIGenericError, ex, "The service account could not be granted 'logon as a service right'");
                    if (await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not grant the 'logon as a service' right to the account. Do you want to continue?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Negative)
                    {
                        return;
                    }
                }

                if (o is IGroupManagedServiceAccount)
                {
                    if (!this.windowsServiceProvider.CanGmsaBeUsedOnThisMachine(o.SamAccountName))
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The managed service account is not able to be used on this machine. Use the Test-AdServiceAccount PowerShell cmdlet to find out more and resolve the issue before trying again");
                        return;
                    }
                }
                else
                {
                    while (true)
                    {
                        LoginDialogData r = await this.dialogCoordinator.ShowLoginAsync(this, "Service account", "Enter the password for the service account", new LoginDialogSettings
                        {
                            EnablePasswordPreview = true,
                            ShouldHideUsername = true,
                            AffirmativeButtonText = "OK"
                        });

                        if (r?.Password == null)
                        {
                            return;
                        }

                        password = r.Password;

                        var result = this.windowsServiceProvider.LogonServiceAccount(o, password);

                        if (result == 0)
                        {
                            break;
                        }

                        if (result == 1326)
                        {
                            await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The username or password was incorrect");
                            continue;
                        }

                        if (result == 1385)
                        {
                            if (logonAsServiceFailed)
                            {
                                break;
                            }

                            if (await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The user does not have the 'Logon as a service' right on this computer. Do you want to continue anyway?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Negative)
                            {
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }

                        return;
                    }
                }

                this.ServiceAccount = sid;
                this.workingServiceAccountUserName = o.MsDsPrincipalName;
                this.workingServiceAccountPassword = password;

                this.PopulateIsNotGmsa();
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not change the service account\r\n{ex.Message}");
            }
        }

        public void ShowCertificateDialog()
        {
            X509Certificate2UI.DisplayCertificate(this.Certificate, this.GetHandle());
        }

        public void ShowImportDialog()
        {
            X509Certificate2 newCert = NativeMethods.ShowCertificateImportDialog(this.GetHandle(), "Import certificate", StoreLocation.LocalMachine, StoreName.My);

            if (newCert != null)
            {
                this.Certificate = newCert;
                this.certPermissionProvider.AddReadPermission(this.Certificate);
            }
        }

        public void ShowSelectCertificateDialog()
        {
            X509Certificate2Collection results = X509Certificate2UI.SelectFromCollection(this.certProvider.GetEligibleServerAuthenticationCertificates(), "Select TLS certificate", "Select a certificate to use as the TLS certificate for this web site", X509SelectionFlag.SingleSelection, this.GetHandle());

            if (results.Count == 1)
            {
                this.Certificate = results[0];
                this.certPermissionProvider.AddReadPermission(this.Certificate);
            }
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

        private T CloneModel<T>(T model)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(model));
        }

        private bool IsReservationInUse(bool currentlyUnconfigured, string oldurl, string newurl, out string user)
        {
            user = null;

            if (!currentlyUnconfigured && oldurl == newurl)
            {
                return false;
            }

            return this.certificateBindingProvider.IsReservationInUse(newurl, out user);
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

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}