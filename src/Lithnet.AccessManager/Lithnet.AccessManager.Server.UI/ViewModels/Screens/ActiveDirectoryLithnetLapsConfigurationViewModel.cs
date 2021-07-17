using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryLithnetLapsConfigurationViewModel : Screen, IHelpLink
    {
        private readonly ICertificateProvider certificateProvider;
        private readonly IViewModelFactory<X509Certificate2ViewModel, X509Certificate2> certificate2ViewModelFactory;
        private readonly IViewModelFactory<ActiveDirectoryForestSchemaViewModel, Forest> forestFactory;

        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly ILogger<ActiveDirectoryLithnetLapsConfigurationViewModel> logger;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ICertificatePermissionProvider certPermissionProvider;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly INotifyModelChangedEventPublisher eventPublisher;

        public ActiveDirectoryLithnetLapsConfigurationViewModel(IDialogCoordinator dialogCoordinator, ICertificateProvider certificateProvider, IViewModelFactory<X509Certificate2ViewModel, X509Certificate2> certificate2ViewModelFactory, IWindowsServiceProvider windowsServiceProvider, ILogger<ActiveDirectoryLithnetLapsConfigurationViewModel> logger, IShellExecuteProvider shellExecuteProvider, IDomainTrustProvider domainTrustProvider, IDiscoveryServices discoveryServices, IScriptTemplateProvider scriptTemplateProvider, ICertificatePermissionProvider certPermissionProvider, DataProtectionOptions dataProtectionOptions, INotifyModelChangedEventPublisher eventPublisher, IViewModelFactory<ActiveDirectoryForestSchemaViewModel, Forest> forestFactory)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.certificateProvider = certificateProvider;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.windowsServiceProvider = windowsServiceProvider;
            this.logger = logger;
            this.domainTrustProvider = domainTrustProvider;
            this.discoveryServices = discoveryServices;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.dataProtectionOptions = dataProtectionOptions;
            this.eventPublisher = eventPublisher;
            this.forestFactory = forestFactory;

            this.Forests = new BindableCollection<ActiveDirectoryForestSchemaViewModel>();

            this.AvailableCertificates = new BindableCollection<X509Certificate2ViewModel>();
            this.DisplayName = "Lithnet LAPS";
            this.certPermissionProvider = certPermissionProvider;
        }

        public string HelpLink => Constants.HelpLinkPageLocalAdminPasswords;

        protected override void OnInitialActivate()
        {
            Task.Run(async () => await this.Initialize());
        }

        private async Task Initialize()
        {
            try
            {
                await this.PopulateForestsAndDomains();
                this.SelectedForest = this.Forests.FirstOrDefault();
                await this.RefreshAvailableCertificates();
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

        private async Task PopulateForestsAndDomains()
        {
            try
            {
                foreach (Forest forest in this.domainTrustProvider.GetForests())
                {
                    this.Forests.Add(forestFactory.CreateViewModel(forest));
                }

                await this.RefreshData();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Unable to populate forest list");
            }
        }

        private async Task RefreshData()
        {
            foreach (var forest in this.Forests)
            {
                await forest.RefreshSchemaStatusAsync();
            }
        }

        public BindableCollection<ActiveDirectoryForestSchemaViewModel> Forests { get; }

        public ActiveDirectoryForestSchemaViewModel SelectedForest { get; set; }

        public bool CanExtendSchemaLithnetAccessManager => this.SelectedForest?.IsNotLithnetSchemaPresent == true;

        public async Task ExtendSchemaLithnetAccessManager()
        {
            try
            {
                ActiveDirectoryForestSchemaViewModel current = this.SelectedForest;

                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run the following script as an account that is a member of the 'Schema Admins' group",
                    ScriptText = this.scriptTemplateProvider.UpdateAdSchema
                        .Replace("{forest}", current.Name)
                };

                ExternalDialogWindow w = new ExternalDialogWindow
                {
                    Title = "Script",
                    DataContext = vm,
                    SaveButtonVisible = false,
                    CancelButtonName = "Close"
                };

                w.ShowDialog();

                await current.RefreshSchemaStatusAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task RefreshSchemaStatusAsync()
        {
            foreach (var vm in this.Forests)
            {
                await vm.RefreshSchemaStatusAsync();
            }
        }

        private void OnSelectedForestChanged()
        {
            _ = this.RefreshAvailableCertificates();
        }

        public X509Certificate2ViewModel SelectedCertificate { get; set; }

        [PropertyChanged.DependsOn(nameof(SelectedForest))]
        public BindableCollection<X509Certificate2ViewModel> AvailableCertificates { get; }

        public bool CanPublishSelectedCertificate => !this.SelectedCertificate?.IsPublished ?? false;

        public async Task PublishSelectedCertificate()
        {
            try
            {
                var de = this.discoveryServices.GetConfigurationNamingContext(this.SelectedForest.Forest.RootDomain.Name);
                var certData = Convert.ToBase64String(this.SelectedCertificate.Model.RawData, Base64FormattingOptions.InsertLineBreaks);

                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run the following script to publish the encryption certificate",
                    ScriptText = this.scriptTemplateProvider.PublishLithnetAccessManagerCertificate
                        .Replace("{configurationNamingContext}", de.GetPropertyString("distinguishedName"))
                        .Replace("{certificateData}", certData)
                        .Replace("{forest}", this.SelectedForest.Name)
                };

                ExternalDialogWindow w = new ExternalDialogWindow
                {
                    Title = "Script",
                    DataContext = vm,
                    SaveButtonVisible = false,
                    CancelButtonName = "Close"
                };

                w.ShowDialog();

                try
                {
                    if (this.certificateProvider.TryGetCertificateFromDirectory(out X509Certificate2 publishedCert,
                        this.SelectedForest.Forest.RootDomain.Name))
                    {
                        if (publishedCert.Thumbprint == this.SelectedCertificate.Model.Thumbprint)
                        {
                            this.SelectedCertificate.IsPublished = true;

                            foreach (var c in this.AvailableCertificates.ToList())
                            {
                                if (this.SelectedCertificate != c)
                                {
                                    c.IsPublished = false;
                                }

                                if (c.IsOrphaned)
                                {
                                    this.AvailableCertificates.Remove(c);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not update certificate publication information");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanGenerateEncryptionCertificate { get; set; } = true;

        public async Task GenerateEncryptionCertificate()
        {
            try
            {
                X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert(this.SelectedForest.Name, CertificateProvider.LithnetAccessManagerAdPasswordEncryptionEku);

                using X509Store store = X509ServiceStoreHelper.Open(AccessManager.Constants.ServiceName, OpenFlags.ReadWrite);
                store.Add(cert);

                this.certPermissionProvider.AddReadPermission(cert);

                var vm = this.certificate2ViewModelFactory.CreateViewModel(cert);

                this.AvailableCertificates.Add(vm);
                this.SelectedCertificate = vm;

                this.NotifyCertificateListChanged();

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Encryption certificate created", "A new certificate has been generated. Publish this certificate to the directory to allow clients to encrypt passwords with this certificate.\r\n\r\n Note, that if you lose this certificate, passwords encrypted with it will not be recoverable.\r\n\r\n Do you want to backup the encryption certificate now?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" }) == MessageDialogResult.Affirmative)
                {
                    await this.ExportCertificate();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not generate encryption certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not generate the certificate\r\n{ex.Message}");
            }
        }

        public bool CanRepermission => this.SelectedCertificate?.CanRepermission == true;

        public async Task Repermission()
        {
            try
            {
                var cert = this.SelectedCertificate;

                if (cert == null)
                {
                    return;
                }

                cert.Repermission();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanShowCertificateDialog => this.SelectedCertificate != null;

        public async Task ShowCertificateDialog()
        {
            try
            {
                X509Certificate2UI.DisplayCertificate(this.SelectedCertificate.Model, this.GetHandle());
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanExportCertificate => this.SelectedCertificate != null && this.SelectedCertificate.HasPrivateKey;

        public async Task ExportCertificate()
        {
            try
            {
                var cert = this.SelectedCertificate.Model;

                if (cert != null && cert.HasPrivateKey)
                {
                    NativeMethods.ShowCertificateExportDialog(this.GetHandle(), "Export certificate", cert);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanDeleteCertificate => this.SelectedCertificate != null;

        public async Task DeleteCertificate()
        {
            var cert = this.SelectedCertificate.Model;

            try
            {
                if (cert != null)
                {
                    if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", "If you delete this certificate, you will no longer be able to decrypt any passwords that have been encrypted with it. Are you sure you want to proceed?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Yes",
                        NegativeButtonText = "No"
                    }) == MessageDialogResult.Affirmative)
                    {
                        using (X509Store store = X509ServiceStoreHelper.Open(AccessManager.Constants.ServiceName, OpenFlags.ReadWrite))
                        {
                            store.Remove(cert);
                            this.NotifyCertificateListChanged();
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not delete certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not delete the certificate\r\n{ex.Message}");
            }
            finally
            {
                await this.RefreshAvailableCertificates();
            }
        }

        public async Task ImportCertificate()
        {
            try
            {
                using (X509Store store = X509ServiceStoreHelper.Open(AccessManager.Constants.ServiceName, OpenFlags.ReadWrite))
                {
                    X509Certificate2 newCert = NativeMethods.ShowCertificateImportDialog(this.GetHandle(), "Import encryption certificate", store);

                    if (newCert != null)
                    {
                        this.certPermissionProvider.AddReadPermission(newCert);
                        this.NotifyCertificateListChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not import certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not import the certificate\r\n{ex.Message}");
            }
            finally
            {
                await this.RefreshAvailableCertificates();
            }
        }

        public async Task DelegateServicePermission()
        {
            try
            {
                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Modify the OU variable in this script, and run it with domain admin rights to assign permissions for the service account to be able to read the encrypted local admin passwords and history from the directory",
                    ScriptText = this.scriptTemplateProvider.GrantAccessManagerPermissions.Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceAccountSid().ToString(), StringComparison.OrdinalIgnoreCase)
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
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task OpenAccessManagerAgentDownload()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.LinkDownloadAccessManagerAgent);
        }

        private void NotifyCertificateListChanged()
        {
            if (this.dataProtectionOptions.EnableCertificateSynchronization)
            {
                this.eventPublisher.RaiseModelChangedEvent(this, "Certificates", false);
            }
        }

        private async Task RefreshAvailableCertificates()
        {
            try
            {
                if (this.AvailableCertificates == null)
                {
                    return;
                }

                this.AvailableCertificates.Clear();

                if (this.SelectedForest == null)
                {
                    return;
                }

                var allCertificates = certificateProvider.GetEligibleAdPasswordEncryptionCertificates(false).OfType<X509Certificate2>();
                this.certificateProvider.TryGetCertificateFromDirectory(out X509Certificate2 publishedCert, this.SelectedForest.Forest.RootDomain.Name);

                bool foundPublished = false;

                foreach (var certificate in allCertificates)
                {
                    var vm = this.certificate2ViewModelFactory.CreateViewModel(certificate);

                    if (certificate.Thumbprint == publishedCert?.Thumbprint)
                    {
                        vm.IsPublished = true;
                        foundPublished = true;
                    }

                    if (certificate.Subject.StartsWith($"CN={this.SelectedForest.Forest.RootDomain.Name}",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        this.AvailableCertificates.Add(vm);
                    }
                }

                if (!foundPublished && publishedCert != null)
                {
                    var vm = this.certificate2ViewModelFactory.CreateViewModel(publishedCert);
                    vm.IsOrphaned = true;
                    vm.IsPublished = true;
                    this.AvailableCertificates.Add(vm);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not load certificate list");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not refresh the certificate list\r\n{ex.Message}");
            }
        }

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public async Task OpenLapsStrategyLink()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkPageChooseLapsStrategy);
        }
    }
}
