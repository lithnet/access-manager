using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stylet;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class HostingViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<HostingViewModel> logger;
        private readonly IAppPathProvider pathProvider;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly ICertificateProvider certProvider;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IActiveDirectory directory;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ICertificatePermissionProvider certPermissionProvider;
        private readonly IRegistryProvider registryProvider;
        private readonly ISecretRekeyProvider rekeyProvider;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly IHttpSysConfigurationProvider certificateBindingProvider;
        private readonly IFirewallProvider firewallProvider;
        private readonly TokenIssuerOptions tokenIssuerOptions;
        private readonly IProtectedSecretProvider protectedSecretProvider;
        private readonly RandomNumberGenerator csp;
        private readonly IWindowManager windowManager;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;

        public HostingViewModel(HostingOptions model, IDialogCoordinator dialogCoordinator, IWindowsServiceProvider windowsServiceProvider, ILogger<HostingViewModel> logger, IModelValidator<HostingViewModel> validator, IAppPathProvider pathProvider, INotifyModelChangedEventPublisher eventPublisher, ICertificateProvider certProvider, IShellExecuteProvider shellExecuteProvider, IEventAggregator eventAggregator, IActiveDirectory directory, IScriptTemplateProvider scriptTemplateProvider, ICertificatePermissionProvider certPermissionProvider, IRegistryProvider registryProvider, ISecretRekeyProvider rekeyProvider, IObjectSelectionProvider objectSelectionProvider, IHttpSysConfigurationProvider certificateBindingProvider, IFirewallProvider firewallProvider, TokenIssuerOptions tokenIssuerOptions, IProtectedSecretProvider protectedSecretProvider, RandomNumberGenerator csp, IViewModelFactory<EnterpriseEditionBadgeViewModel, EnterpriseEditionBadgeModel> enterpriseEditionViewModelFactory, IWindowManager windowManager, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory)
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
            this.certificateBindingProvider = certificateBindingProvider;
            this.firewallProvider = firewallProvider;
            this.tokenIssuerOptions = tokenIssuerOptions;
            this.protectedSecretProvider = protectedSecretProvider;
            this.csp = csp;
            this.windowManager = windowManager;
            this.externalDialogWindowFactory = externalDialogWindowFactory;

            this.WorkingModel = this.CloneModel(model);
            this.Certificate = this.certificateBindingProvider.GetCertificate();
            this.OriginalCertificate = this.Certificate;
            this.ServiceAccount = this.windowsServiceProvider.GetServiceAccountSid();
            this.ApiEnabled = this.registryProvider.ApiEnabled;
            this.DisplayName = "Service host";

            eventPublisher.Register(this);

            this.EnterpriseEdition = enterpriseEditionViewModelFactory.CreateViewModel(new EnterpriseEditionBadgeModel
            {
                ToolTipText = "Access Manager API is an enterprise edition feature. Click to learn more",
                RequiredFeature = Enterprise.LicensedFeatures.AmsApi,
                Link = Constants.EnterpriseEditionLearnMoreLinkAmsApi
            });
        }

        public EnterpriseEditionBadgeViewModel EnterpriseEdition { get; set; }

        public string HelpLink => Constants.HelpLinkPageWebHosting;

        protected override void OnInitialActivate()
        {
            this.PopulateCanDelegate();
            this.PopulateIsNotGmsa();
        }

        public bool CanShowCertificateDialog => this.Certificate != null;

        [NotifyModelChangedProperty]
        public X509Certificate2 Certificate { get; set; }

        public string CertificateDisplayName => this.Certificate.ToDisplayName();

        public string CertificateExpiryText { get; set; }

        [NotifyModelChangedProperty]
        public string Hostname { get => this.WorkingModel.HttpSys.Hostname; set => this.WorkingModel.HttpSys.Hostname = value; }

        [NotifyModelChangedProperty]
        public int HttpPort { get => this.WorkingModel.HttpSys.HttpPort; set => this.WorkingModel.HttpSys.HttpPort = value; }

        [NotifyModelChangedProperty]
        public int HttpsPort { get => this.WorkingModel.HttpSys.HttpsPort; set => this.WorkingModel.HttpSys.HttpsPort = value; }

        [NotifyModelChangedProperty]
        public bool ApiEnabled { get; set; }

        [NotifyModelChangedProperty]
        public string ApiHostname { get => this.WorkingModel.HttpSys.ApiHostname; set => this.WorkingModel.HttpSys.ApiHostname = value; }

        public PackIconMaterialKind Icon => PackIconMaterialKind.Web;

        public bool IsCertificateCurrent { get; set; }

        public bool IsCertificateExpired { get; set; }

        public bool IsCertificateExpiring { get; set; }

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

        public async Task PreventDelegation()
        {
            try
            {
                ScriptContentViewModel vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run the following script as an account that is a member of the 'Domain admins' group",
                    ScriptText = this.scriptTemplateProvider.PreventDelegation
                        .Replace("{sid}", this.ServiceAccount.ToString(), StringComparison.OrdinalIgnoreCase)
                };

                var evm = this.externalDialogWindowFactory.CreateViewModel(vm);

                windowManager.ShowDialog(evm);

                this.PopulateCanDelegate();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task CreateGmsa()
        {
            try
            {
                ScriptContentViewModel vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run the following script as an account that is a member of the 'Domain admins' group",
                    ScriptText = this.scriptTemplateProvider.CreateGmsa
                        .Replace("{serverName}", Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                };
                var evm = this.externalDialogWindowFactory.CreateViewModel(vm);

                windowManager.ShowDialog(evm);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool ShowCertificateExpiryWarning => this.Certificate != null && this.Certificate.NotAfter.AddDays(-30) >= DateTime.Now;

        private X509Certificate2 OriginalCertificate { get; set; }

        private HostingOptions OriginalModel { get; set; }

        private HostingOptions WorkingModel { get; set; }

        private string workingServiceAccountPassword;

        private string workingServiceAccountUserName;

        public async Task<bool> CommitSettings(object dialogContext = null)
        {
            dialogContext ??= this;

            if (this.Certificate == null)
            {
                await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", "You must select a HTTPS certificate");
                return false;
            }

            if (this.ApiEnabled && string.IsNullOrWhiteSpace(this.WorkingModel.HttpSys.ApiHostname))
            {
                await this.dialogCoordinator.ShowMessageAsync(dialogContext, "Error", "You must specify the fully qualified DNS hostname that clients will use to connect to the API");
                return false;
            }

            bool currentlyUnconfigured = !this.registryProvider.IsConfigured;

            bool updateHttpReservations =
                this.WorkingModel.HttpSys.Hostname != this.OriginalModel.HttpSys.Hostname ||
                this.WorkingModel.HttpSys.HttpPort != this.OriginalModel.HttpSys.HttpPort ||
                this.WorkingModel.HttpSys.HttpsPort != this.OriginalModel.HttpSys.HttpsPort ||
                currentlyUnconfigured;

            bool updateApi =
                this.WorkingModel.HttpSys.ApiHostname != this.OriginalModel.HttpSys.ApiHostname ||
                this.ApiEnabled != this.registryProvider.ApiEnabled;

            bool updateConfigFile = updateHttpReservations || updateApi;

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
                if (updateApi)
                {
                    this.registryProvider.ApiEnabled = this.ApiEnabled;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIConfigurationSaveError, ex, "Could not change the API enabled state");
                rollbackContext.Rollback(this.logger);
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
                    await this.UpdateServiceAccount();

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
            }

            if (updateCertificateBinding || updateHttpReservations || updateServiceAccount || updateConfigFile || updateFirewallRules || updateApi)
            {
                this.OriginalModel = this.CloneModel(this.WorkingModel);
                this.OriginalCertificate = this.Certificate;
                this.eventAggregator.Publish(new ModelChangedEvent(this, "Hosting", true));
            }

            return true;
        }

        private async Task UpdateServiceAccount()
        {
            try
            {
                this.windowsServiceProvider.SetServiceAccount(this.workingServiceAccountUserName, this.workingServiceAccountPassword);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        private void SaveHostingConfigFile(HostingSettingsRollbackContext rollback)
        {
            HostingOptions originalSettings = this.OriginalModel;

            this.WorkingModel.Save(pathProvider.HostingConfigFile);
            rollback.RollbackActions.Add(() => originalSettings.Save(pathProvider.HostingConfigFile));
        }

        public void OnCertificateChanged()
        {
            try
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
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
            }
        }

        public void OnApiEnabledChanged()
        {
            if (this.ApiEnabled)
            {
                if (this.tokenIssuerOptions.SigningKey == null)
                {
                    byte[] buffer = new byte[128];
                    this.csp.GetBytes(buffer);
                    this.tokenIssuerOptions.SigningKey = this.protectedSecretProvider.ProtectSecret(Convert.ToBase64String(buffer));
                    this.tokenIssuerOptions.SigningAlgorithm = "HS512";
                }
            }
        }

        public async Task SelectServiceAccountUser()
        {
            try
            {
                if (!this.objectSelectionProvider.GetUserOrServiceAccount(this, out SecurityIdentifier sid))
                {
                    return;
                }

                if (!directory.TryGetPrincipal(sid, out IActiveDirectorySecurityPrincipal o))
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

                if (o is IActiveDirectoryGroupManagedServiceAccount)
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
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not change service account");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not change the service account\r\n{ex.Message}");
            }
        }

        public async Task ShowCertificateDialog()
        {
            try
            {
                X509Certificate2UI.DisplayCertificate(this.Certificate, this.GetHandle());
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task ShowImportDialog()
        {
            try
            {
                X509Certificate2 newCert = NativeMethods.ShowCertificateImportDialog(this.GetHandle(), "Import certificate", StoreLocation.LocalMachine, StoreName.My);

                if (newCert != null)
                {
                    this.Certificate = newCert;
                    this.certPermissionProvider.AddReadPermission(this.Certificate);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task ShowSelectCertificateDialog()
        {
            try
            {
                X509Certificate2Collection results = X509Certificate2UI.SelectFromCollection(this.certProvider.GetEligibleServerAuthenticationCertificates(), "Select TLS certificate", "Select a certificate to use as the TLS certificate for this web site", X509SelectionFlag.SingleSelection, this.GetHandle());

                if (results.Count == 1)
                {
                    this.Certificate = results[0];
                    this.certPermissionProvider.AddReadPermission(this.Certificate);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        private T CloneModel<T>(T model)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(model));
        }

        private bool IsReservationInUse(bool currentlyUnconfigured, string oldUrl, string newUrl, out string user)
        {
            user = null;

            if (!currentlyUnconfigured && oldUrl == newUrl)
            {
                return false;
            }

            return this.certificateBindingProvider.IsReservationInUse(newUrl, out user);
        }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}