using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Api.Configuration;
using MahApps.Metro.IconPacks;
using Stylet;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    public class AdvancedPasswordManagementViewModel : Screen, IHelpLink
    {
        private readonly ApiAuthenticationOptions agentOptions;
        private readonly AmsManagedDeviceRegistrationOptions amsManagedDeviceOptions;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ICertificateProvider certificateProvider;
        private readonly IX509Certificate2ViewModelFactory certificate2ViewModelFactory;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<LapsConfigurationViewModel> logger;
        private readonly ICertificatePermissionProvider certPermissionProvider;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly PasswordPolicyOptions passwordOptions;

        public AdvancedPasswordManagementViewModel(INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, ApiAuthenticationOptions agentOptions, AmsManagedDeviceRegistrationOptions amsManagedDeviceOptions, ICertificateProvider certificateProvider, IX509Certificate2ViewModelFactory certificate2ViewModelFactory, IDialogCoordinator dialogCoordinator, ILogger<LapsConfigurationViewModel> logger, ICertificatePermissionProvider certPermissionProvider, DataProtectionOptions dataProtectionOptions, PasswordPolicyOptions passwordOptions)
        {
            this.eventPublisher = eventPublisher;
            this.shellExecuteProvider = shellExecuteProvider;
            this.agentOptions = agentOptions;
            this.amsManagedDeviceOptions = amsManagedDeviceOptions;
            this.certificateProvider = certificateProvider;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.certPermissionProvider = certPermissionProvider;
            this.dataProtectionOptions = dataProtectionOptions;
            this.passwordOptions = passwordOptions;

            this.AvailableCertificates = new BindableCollection<X509Certificate2ViewModel>();
            this.DisplayName = "API";
            eventPublisher.Register(this);
        }

        protected override void OnInitialActivate()
        {
            Task.Run(async () =>
            {
                await this.RefreshAvailableCertificates();
            });
        }

        public X509Certificate2ViewModel SelectedCertificate { get; set; }

        public BindableCollection<X509Certificate2ViewModel> AvailableCertificates { get; }

        public string HelpLink => Constants.HelpLinkPageEmail;

        [NotifyModelChangedProperty]
        public bool AllowAzureAdJoinedDevices { get => this.agentOptions.AllowAzureAdJoinedDeviceAuth; set => this.agentOptions.AllowAzureAdJoinedDeviceAuth = value; }

        [NotifyModelChangedProperty]
        public bool AllowAzureAdRegisteredDevices { get => this.agentOptions.AllowAzureAdRegisteredDeviceAuth; set => this.agentOptions.AllowAzureAdRegisteredDeviceAuth = value; }

        [NotifyModelChangedProperty]
        public bool AllowAmsManagedDeviceAuth { get => this.agentOptions.AllowAmsManagedDeviceAuth; set => this.agentOptions.AllowAmsManagedDeviceAuth = value; }

        [NotifyModelChangedProperty]
        public bool AutoApproveNewDevices { get => this.amsManagedDeviceOptions.AutoApproveNewDevices; set => this.amsManagedDeviceOptions.AutoApproveNewDevices = value; }

        [NotifyModelChangedProperty]
        [AlsoNotifyFor(nameof(CanSetActiveCertificate))]
        public string ActiveCertificateThumbprint
        {
            get => this.passwordOptions.EncryptionCertificateThumbprint; set => this.passwordOptions.EncryptionCertificateThumbprint = value;
        }

        public PackIconUniconsKind Icon => PackIconUniconsKind.ServerConnection;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public bool CanSetActiveCertificate => !this.SelectedCertificate?.IsPublished ?? false;

        public void SetActiveCertificate()
        {
            var selectedCertificate = this.SelectedCertificate;

            if (selectedCertificate == null)
            {
                return;
            }

            selectedCertificate.IsPublished = true;
            this.ActiveCertificateThumbprint = selectedCertificate.Model.Thumbprint;

            foreach (var c in this.AvailableCertificates.ToList())
            {
                if (selectedCertificate != c)
                {
                    c.IsPublished = false;
                }

                if (c.IsOrphaned)
                {
                    this.AvailableCertificates.Remove(c);
                }
            }
        }

        public bool CanGenerateEncryptionCertificate { get; set; } = true;

        public async Task GenerateEncryptionCertificate()
        {
            try
            {
                X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert("Lithnet Access Manager password encryption", CertificateProvider.LithnetAccessManagerPasswordEncryptionEku);

                using X509Store store = X509ServiceStoreHelper.Open(AccessManager.Constants.ServiceName, OpenFlags.ReadWrite);
                store.Add(cert);

                this.certPermissionProvider.AddReadPermission(cert);

                var vm = this.certificate2ViewModelFactory.CreateViewModel(cert);

                this.AvailableCertificates.Add(vm);
                this.SelectedCertificate = vm;

                this.NotifyCertificateListChanged();

                if (await this.dialogCoordinator.ShowMessageAsync(this, "Encryption certificate created", "A new certificate has been generated. Set this certificate to active to allow clients to encrypt passwords with this certificate.\r\n\r\n Note, that if you lose this certificate, passwords encrypted with it will not be recoverable.\r\n\r\n Do you want to backup the encryption certificate now?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" }) == MessageDialogResult.Affirmative)
                {
                    this.ExportCertificate();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not generate encryption certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not generate the certificate\r\n{ex.Message}");
            }
        }

        public bool CanRepermission => this.SelectedCertificate?.CanRepermission == true;

        public void Repermission()
        {
            var cert = this.SelectedCertificate;

            if (cert == null)
            {
                return;
            }

            cert.Repermission();
        }

        public bool CanShowCertificateDialog => this.SelectedCertificate != null;

        public void ShowCertificateDialog()
        {
            X509Certificate2UI.DisplayCertificate(this.SelectedCertificate.Model, this.GetHandle());
        }

        public bool CanExportCertificate => this.SelectedCertificate != null && this.SelectedCertificate.HasPrivateKey;

        public void ExportCertificate()
        {
            var cert = this.SelectedCertificate.Model;

            if (cert != null && cert.HasPrivateKey)
            {
                NativeMethods.ShowCertificateExportDialog(this.GetHandle(), "Export certificate", cert);
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

                var allCertificates = certificateProvider.GetEligiblePasswordEncryptionCertificates(false).OfType<X509Certificate2>();

                X509Certificate2 publishedCert = null;

                try
                {
                    if (this.ActiveCertificateThumbprint != null)
                    {
                        publishedCert = this.certificateProvider.FindDecryptionCertificate(this.ActiveCertificateThumbprint);
                    }
                }
                catch
                {
                }

                bool foundPublished = false;

                foreach (var certificate in allCertificates)
                {
                    var vm = this.certificate2ViewModelFactory.CreateViewModel(certificate);

                    if (certificate.Thumbprint == publishedCert?.Thumbprint)
                    {
                        vm.IsPublished = true;
                        foundPublished = true;
                    }

                    this.AvailableCertificates.Add(vm);
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
    }
}
