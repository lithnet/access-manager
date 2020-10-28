using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SslCertBinding.Net;
using Stylet;
using WindowsFirewallHelper;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class HostingViewModel : Screen, IHelpLink
    {
        private const string SddlTemplate = "D:(A;;GX;;;{0})";

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
        private readonly IClusterProvider clusterProvider;

        public HostingViewModel(HostingOptions model, IDialogCoordinator dialogCoordinator, IWindowsServiceProvider windowsServiceProvider, ILogger<HostingViewModel> logger, IModelValidator<HostingViewModel> validator, IAppPathProvider pathProvider, INotifyModelChangedEventPublisher eventPublisher, ICertificateProvider certProvider, IShellExecuteProvider shellExecuteProvider, IEventAggregator eventAggregator, IDirectory directory, IScriptTemplateProvider scriptTemplateProvider, ICertificatePermissionProvider certPermissionProvider, IRegistryProvider registryProvider, IClusterProvider clusterProvider)
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
            this.clusterProvider = clusterProvider;

            this.WorkingModel = this.CloneModel(model);
            this.Certificate = this.GetCertificate();
            this.OriginalCertificate = this.Certificate;
            this.ServiceAccount = this.windowsServiceProvider.GetServiceAccount();
            this.OriginalServiceAccount = this.ServiceAccount;
            this.ServiceStatus = this.windowsServiceProvider.Status.ToString();
            this.DisplayName = "Web hosting";

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

                using PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
                using UserPrincipal u = UserPrincipal.FindByIdentity(ctx, IdentityType.Sid, this.ServiceAccount.ToString());

                if (u != null)
                {
                    this.CanBeDelegated = u.DelegationPermitted;
                    return;
                }

                using ComputerPrincipal cmp = ComputerPrincipal.FindByIdentity(ctx, IdentityType.Sid, this.ServiceAccount.ToString());

                if (cmp != null)
                {
                    this.CanBeDelegated = cmp.DelegationPermitted;
                }
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

                using PrincipalContext ctx = new PrincipalContext(ContextType.Domain);
                using ComputerPrincipal cmp = ComputerPrincipal.FindByIdentity(ctx, IdentityType.Sid, this.ServiceAccount.ToString());

                if (cmp != null)
                {
                    this.IsNotGmsa = !string.Equals(cmp.StructuralObjectClass, "msDS-GroupManagedServiceAccount", StringComparison.OrdinalIgnoreCase);
                }
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

        private SecurityIdentifier OriginalServiceAccount { get; set; }

        private HostingOptions WorkingModel { get; set; }

        private string workingServiceAccountPassword { get; set; }

        private string workingServiceAccountUserName { get; set; }

        private void UpdateIsConfigured()
        {
            this.IsConfigured = registryProvider.IsConfigured;
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        public async Task<bool> CommitSettings()
        {
            if (this.Certificate == null)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", "You must select a HTTPS certificate");
                return false;
            }

            this.UpdateIsConfigured();

            bool currentlyUnconfigured = this.IsUnconfigured;

            bool updatePrivateKeyPermissions =
                this.ServiceAccount != this.OriginalServiceAccount ||
                this.Certificate?.Thumbprint != this.OriginalCertificate?.Thumbprint ||
                currentlyUnconfigured;

            bool updateHttpReservations =
                this.WorkingModel.HttpSys.Hostname != this.OriginalModel.HttpSys.Hostname ||
                this.WorkingModel.HttpSys.HttpPort != this.OriginalModel.HttpSys.HttpPort ||
                this.WorkingModel.HttpSys.HttpsPort != this.OriginalModel.HttpSys.HttpsPort ||
                this.ServiceAccount != this.OriginalServiceAccount ||
                currentlyUnconfigured;

            bool updateConfigFile = updateHttpReservations;

            bool updateFirewallRules = updateHttpReservations;

            bool updateCertificateBinding =
                this.WorkingModel.HttpSys.HttpsPort != this.OriginalModel.HttpSys.HttpsPort ||
                this.Certificate?.Thumbprint != this.OriginalCertificate?.Thumbprint ||
                currentlyUnconfigured;

            bool updateServiceAccount = this.workingServiceAccountUserName != null;

            bool updateFileSystemPermissions = updateServiceAccount || currentlyUnconfigured;

            HostingSettingsRollbackContext rollbackContext = new HostingSettingsRollbackContext();
            rollbackContext.StartingUnconfigured = currentlyUnconfigured;

            try
            {
                if (updatePrivateKeyPermissions)
                {
                    this.UpdateCertificatePermissions(rollbackContext);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Could not add private key permissions for SSL certificate");
                MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred while trying to add permissions for the service account {this.ServiceAccountDisplayName} to read the private key of the specified certificate. Try adding permissions for this manually using the Windows computer certificates MMC console. Do you want to continue with the operation?\r\n{ex.Message}", MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Canceled)
                {
                    return false;
                }
            }

            try
            {
                if (updatePrivateKeyPermissions)
                {
                    this.UpdateEncryptionCertificateAcls(rollbackContext);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Could not add private key permissions for encryption certificate");
                MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred while trying to add permissions for the service account {this.ServiceAccountDisplayName} to read the private key of one of the existing password encryption certificates. Try adding permissions for this manually using the Windows service certificates MMC console. Do you want to continue with the operation?\r\n{ex.Message}", MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Canceled)
                {
                    return false;
                }
            }

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
                        MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(this, "Warning", $"The HTTP URL '{this.WorkingModel.HttpSys.BuildHttpUrlPrefix()}' is already registered to user {user}. Do you want to overwrite it?", MessageDialogStyle.AffirmativeAndNegative);

                        if (result == MessageDialogResult.Negative)
                        {
                            return false;
                        }
                    }

                    if (this.IsReservationInUse(currentlyUnconfigured, httpsOld, httpsNew, out user))
                    {
                        MessageDialogResult result = await this.dialogCoordinator.ShowMessageAsync(this, "Warning", $"The HTTPS URL '{this.WorkingModel.HttpSys.BuildHttpsUrlPrefix()}' is already registered to user {user}. Do you want to overwrite it?", MessageDialogStyle.AffirmativeAndNegative);

                        if (result == MessageDialogResult.Negative)
                        {
                            return false;
                        }
                    }

                    this.CreateNewHttpReservations(rollbackContext);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Error creating HTTP reservations");
                rollbackContext.Rollback(this.logger);
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not create the HTTP reservations\r\n{ex.Message}");
                return false;
            }

            try
            {
                if (updateFirewallRules)
                {
                    this.ReplaceFirewallRules(rollbackContext);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Error updating the firewall rules");
                rollbackContext.Rollback(this.logger);

                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not update the firewall rules. Please manually update them to ensure your users can access the application\r\n{ex.Message}");
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
                    this.UpdateCertificateBinding(rollbackContext);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Error creating certificate binding");
                rollbackContext.Rollback(this.logger);
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not bind the certificate to the specified port\r\n{ex.Message}");

                return false;
            }

            try
            {
                if (updateFileSystemPermissions)
                {
                    this.UpdateFileSystemPermissions(rollbackContext);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Error updating file system permissions");
                rollbackContext.Rollback(this.logger);
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not update file system permissions\r\n{ex.Message}");
                return false;
            }

            try
            {
                if (updateServiceAccount)
                {
                    this.UpdateServiceAccount();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex,
                    "Could not change the service account to the specified account {serviceAccountName}",
                    workingServiceAccountUserName);
                rollbackContext.Rollback(this.logger);
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The service account could not be changed\r\n{ex.Message}");
                return false;
            }

            if (currentlyUnconfigured)
            {
                this.registryProvider.IsConfigured = true;
                this.UpdateIsConfigured();
            }

            if (updateCertificateBinding || updateHttpReservations || updatePrivateKeyPermissions ||
                updateServiceAccount || updateConfigFile || updateFirewallRules || updateFileSystemPermissions)
            {
                this.OriginalModel = this.CloneModel(this.WorkingModel);
                this.OriginalCertificate = this.Certificate;
                this.OriginalServiceAccount = this.ServiceAccount;
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

        private void UpdateCertificatePermissions(HostingSettingsRollbackContext rollback)
        {
            if (this.Certificate == null)
            {
                return;
            }

            this.certPermissionProvider.AddReadPermission(this.Certificate, this.ServiceAccount, out Action rollbackAction);
            if (rollbackAction != null)
            {
                rollback.RollbackActions.Add(rollbackAction);
            }

            this.certPermissionProvider.AddReadPermissionToServiceStore(this.ServiceAccount, rollback.RollbackActions);
        }

        public async Task DownloadUpdate()
        {
            try
            {
                if (this.UpdateLink == null)
                {
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = this.UpdateLink,
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not open editor");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not start default browser\r\n{ex.Message}");
            }
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
            LoginDialogData r = await this.dialogCoordinator.ShowLoginAsync(this, "Service account", "Enter the credentials for the service account. If you are using a group-managed service account, leave the password field blank", new LoginDialogSettings
            {
                EnablePasswordPreview = true,
                AffirmativeButtonText = "OK"
            });

            if (r == null)
            {
                return;
            }

            try
            {
                if (directory.TryGetPrincipal(r.Username, out ISecurityPrincipal o) || directory.TryGetPrincipal(r.Username + "$", out o))
                {
                    if (o is IGroup)
                    {
                        throw new DirectoryException("The selected object must be a user");
                    }

                    this.ServiceAccount = o.Sid;
                }
                else
                {
                    using PrincipalContext p = new PrincipalContext(ContextType.Machine);
                    UserPrincipal up = UserPrincipal.FindByIdentity(p, r.Username);

                    if (up == null)
                    {
                        throw new ObjectNotFoundException("The user could not be found");
                    }

                    this.ServiceAccount = up.Sid;
                }

                this.workingServiceAccountUserName = o.MsDsPrincipalName;
                this.workingServiceAccountPassword = r.Password;

                this.PopulateIsNotGmsa();
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The credentials provided could not be validated\r\n{ex.Message}");
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
            }
        }

        public void ShowSelectCertificateDialog()
        {
            X509Certificate2Collection results = X509Certificate2UI.SelectFromCollection(this.GetAvailableCertificateCollection(), "Select TLS certificate", "Select a certificate to use as the TLS certificate for this web site", X509SelectionFlag.SingleSelection, this.GetHandle());

            if (results.Count == 1)
            {
                this.Certificate = results[0];
            }
        }

        public async Task StartService()
        {
            try
            {
                if (this.CanStartService)
                {
                    await this.windowsServiceProvider.StartServiceAsync();
                }
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not start in the requested time");
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not start service");
                await dialogCoordinator.ShowMessageAsync(this, "Service control", $"Could not start service\r\n{ex.Message}");
            }
        }

        public async Task StopService()
        {
            try
            {
                if (this.CanStopService)
                {
                    await this.windowsServiceProvider.StopServiceAsync();

                }
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                await dialogCoordinator.ShowMessageAsync(this, "Service control", "The service did not stop in the requested time");
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not stop service");
                await dialogCoordinator.ShowMessageAsync(this, "Service control", $"Could not stop service\r\n{ex.Message}");
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
                Version currentVersion = Assembly.GetEntryAssembly()?.GetName().Version;
                this.CurrentVersion = currentVersion?.ToString() ?? "Could not determine version";

                string appdata = await DownloadFile(Constants.UrlProductVersionInfo);
                if (appdata != null)
                {
                    PublishedVersionInfo versionInfo = JsonConvert.DeserializeObject<PublishedVersionInfo>(appdata);

                    if (Version.TryParse(versionInfo.CurrentVersion, out Version onlineVersion))
                    {
                        this.AvailableVersion = onlineVersion.ToString();

                        if (onlineVersion > currentVersion)
                        {
                            this.UpdateAvailable = true;
                            this.IsUpToDate = false;
                            this.UpdateLink = versionInfo.UserUrl;
                        }
                        else
                        {
                            this.UpdateAvailable = false;
                            this.IsUpToDate = true;
                            this.UpdateLink = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not get version update");
                this.AvailableVersion = "Unable to determine latest application version";
            }
        }

        private static async Task<string> DownloadFile(string url)
        {
            using HttpClientHandler handler = new HttpClientHandler
            {
                DefaultProxyCredentials = CredentialCache.DefaultCredentials
            };

            using HttpClient client = new HttpClient(handler);
            using HttpResponseMessage result = await client.GetAsync(url);

            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadAsStringAsync();
            }

            return null;
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

            UrlAcl acl = this.GetUrlReservation(newurl);

            if (acl == null)
            {
                return false;
            }

            SecurityIdentifier currentOwner = null;

            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, acl.Sddl);
            foreach (CommonAce dacl in sd.DiscretionaryAcl.OfType<CommonAce>())
            {
                if (dacl.SecurityIdentifier == this.ServiceAccount ||
                    dacl.SecurityIdentifier == this.OriginalServiceAccount)
                {
                    return false;
                }

                currentOwner = dacl.SecurityIdentifier;
            }

            if (currentOwner == null)
            {
                return false;
            }

            try
            {
                user = ((NTAccount)currentOwner.Translate(typeof(NTAccount))).Value;
            }
            catch
            {
                user = currentOwner.ToString();
            }

            return true;
        }


        private void CreateNewHttpReservations(HostingSettingsRollbackContext rollback)
        {
            if (this.ServiceAccount == null)
            {
                return;
            }

            string httpOld = this.OriginalModel.HttpSys.BuildHttpUrlPrefix();
            string httpsOld = this.OriginalModel.HttpSys.BuildHttpsUrlPrefix();
            string httpNew = this.WorkingModel.HttpSys.BuildHttpUrlPrefix();
            string httpsNew = this.WorkingModel.HttpSys.BuildHttpsUrlPrefix();

            UrlAcl existingHttpOld = this.GetUrlReservation(httpOld);
            if (existingHttpOld != null)
            {
                UrlAcl.Delete(existingHttpOld.Prefix);
                rollback.RollbackActions.Add(() => UrlAcl.Create(existingHttpOld.Prefix, existingHttpOld.Sddl));
            }

            UrlAcl existingHttpsOld = this.GetUrlReservation(httpsOld);
            if (existingHttpsOld != null)
            {
                UrlAcl.Delete(existingHttpsOld.Prefix);
                rollback.RollbackActions.Add(() => UrlAcl.Create(existingHttpsOld.Prefix, existingHttpsOld.Sddl));
            }

            UrlAcl existingHttpNew = this.GetUrlReservation(httpNew);
            if (existingHttpNew != null)
            {
                UrlAcl.Delete(existingHttpNew.Prefix);
                rollback.RollbackActions.Add(() => UrlAcl.Create(existingHttpNew.Prefix, existingHttpNew.Sddl));
            }

            UrlAcl existingHttpsNew = this.GetUrlReservation(httpsNew);
            if (existingHttpsNew != null)
            {
                UrlAcl.Delete(existingHttpsNew.Prefix);
                rollback.RollbackActions.Add(() => UrlAcl.Create(existingHttpsNew.Prefix, existingHttpsNew.Sddl));
            }

            this.CreateUrlReservation(httpNew, this.ServiceAccount);
            rollback.RollbackActions.Add(() => UrlAcl.Delete(httpNew));
            this.registryProvider.HttpAcl = httpNew;

            rollback.RollbackActions.Add(() => this.registryProvider.HttpAcl = httpOld);

            this.CreateUrlReservation(httpsNew, this.ServiceAccount);
            rollback.RollbackActions.Add(() => UrlAcl.Delete(httpsNew));
            this.registryProvider.HttpsAcl = httpsNew;
            rollback.RollbackActions.Add(() => this.registryProvider.HttpsAcl = httpsOld);
        }

        private void CreateUrlReservation(string url, SecurityIdentifier sid)
        {
            UrlAcl.Create(url, string.Format(SddlTemplate, sid));
        }

        private UrlAcl GetUrlReservation(string url)
        {
            foreach (UrlAcl acl in UrlAcl.GetAllBindings())
            {
                if (string.Equals(acl.Prefix, url, StringComparison.OrdinalIgnoreCase))
                {
                    return acl;
                }
            }

            return null;
        }

        private void UpdateEncryptionCertificateAcls(HostingSettingsRollbackContext rollback)
        {
            foreach (X509Certificate2 cert in this.certProvider.GetEligiblePasswordEncryptionCertificates(true))
            {
                FileSecurity originalCertificateSecurity = cert.GetPrivateKeySecurity();

                cert.AddPrivateKeyReadPermission(this.ServiceAccount);
                rollback.RollbackActions.Add(() => cert.SetPrivateKeySecurity(originalCertificateSecurity));
            }
        }

        private X509Certificate2Collection GetAvailableCertificateCollection()
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
            Oid serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1");

            foreach (X509Certificate2 c in store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false).OfType<X509Certificate2>().Where(t => t.HasPrivateKey))
            {
                foreach (X509EnhancedKeyUsageExtension x in c.Extensions.OfType<X509EnhancedKeyUsageExtension>())
                {
                    foreach (Oid o in x.EnhancedKeyUsages)
                    {
                        if (o.Value == serverAuthOid.Value)
                        {
                            certs.Add(c);
                        }
                    }
                }
            }

            return certs;
        }

        private X509Certificate2 GetCertificate()
        {
            foreach (CertificateBinding binding in this.GetCertificateBindings())
            {
                if (binding.AppId == HttpSysHostingOptions.AppId)
                {
                    return this.GetCertificateFromStore(binding.StoreName, binding.Thumbprint);
                }
            }

            return null;
        }

        private List<CertificateBinding> GetCertificateBindings()
        {
            CertificateBindingConfiguration config = new CertificateBindingConfiguration();
            CertificateBinding[] results = config.Query();

            return results.ToList();
        }

        private X509Certificate2 GetCertificateFromStore(string storeName, string thumbprint)
        {
            X509Store store = new X509Store(storeName, StoreLocation.LocalMachine, OpenFlags.ReadOnly);

            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false).OfType<X509Certificate2>().FirstOrDefault();
        }

        private void ReplaceFirewallRules(HostingSettingsRollbackContext rollback)
        {
            this.DeleteFirewallRules(rollback);

            IRule firewallRule = CreateNetFwRule((ushort)this.HttpPort, (ushort)this.HttpsPort);

            FirewallManager.Instance.Rules.Add(firewallRule);

            rollback.RollbackActions.Add(() => FirewallManager.Instance.Rules.Remove(firewallRule));
        }

        private IRule CreateNetFwRule(params ushort[] ports)
        {
            IRule firewallRule = FirewallManager.Instance.CreateApplicationRule(FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public,
                Constants.FirewallRuleName,
                FirewallAction.Allow,
               "System",
                FirewallProtocol.TCP
            );

            firewallRule.IsEnable = true;
            firewallRule.Direction = FirewallDirection.Inbound;
            firewallRule.LocalPorts = ports;
            return firewallRule;
        }

        private void DeleteFirewallRules(HostingSettingsRollbackContext rollback)
        {
            try
            {
                IRule existingFirewallRule = FirewallManager.Instance.Rules.SingleOrDefault(t => string.Equals(t.Name, Constants.FirewallRuleName, StringComparison.OrdinalIgnoreCase));
                if (existingFirewallRule != null)
                {
                    FirewallManager.Instance.Rules.Remove(existingFirewallRule);
                    rollback.RollbackActions.Add(() => FirewallManager.Instance.Rules.Add(existingFirewallRule));
                }
            }
            catch
            {
                // ignore
            }
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


        private void UpdateCertificateBinding(HostingSettingsRollbackContext rollback)
        {
            CertificateBindingConfiguration bindingConfiguration = new CertificateBindingConfiguration();
            CertificateBinding originalBinding = this.GetCertificateBinding(bindingConfiguration);

            if (originalBinding != null)
            {
                bindingConfiguration.Delete(originalBinding.IpPort);
                rollback.RollbackActions.Add(() => bindingConfiguration.Bind(originalBinding));
            }

            CertificateBinding binding = new CertificateBinding(this.Certificate.Thumbprint, "My", new IPEndPoint(IPAddress.Parse("0.0.0.0"), this.WorkingModel.HttpSys.HttpsPort), HttpSysHostingOptions.AppId, new BindingOptions());
            bindingConfiguration.Bind(binding);
            rollback.RollbackActions.Add(() => bindingConfiguration.Delete(binding.IpPort));

            this.registryProvider.CertBinding = binding.IpPort.ToString();
            rollback.RollbackActions.Add(() => this.registryProvider.CertBinding = originalBinding?.IpPort?.ToString());
        }

        private void UpdateFileSystemPermissions(HostingSettingsRollbackContext rollback)
        {
            DirectoryInfo di = new DirectoryInfo(this.registryProvider.LogPath);
            DirectorySecurity originalSecurity = di.GetAccessControl();
            di.AddDirectorySecurity(this.ServiceAccount, FileSystemRights.Modify, AccessControlType.Allow);
            rollback.RollbackActions.Add(() => di.SetAccessControl(originalSecurity));
        }

        private CertificateBinding GetCertificateBinding(CertificateBindingConfiguration config)
        {
            foreach (CertificateBinding binding in config.Query())
            {
                if (binding.AppId == HttpSysHostingOptions.AppId)
                {
                    return binding;
                }
            }

            return null;
        }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}