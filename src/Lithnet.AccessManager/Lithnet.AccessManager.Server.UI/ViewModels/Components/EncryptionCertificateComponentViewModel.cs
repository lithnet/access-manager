using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Api;
using MahApps.Metro.IconPacks;
using Stylet;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using PropertyChanged;

namespace Lithnet.AccessManager.Server.UI
{
    public class EncryptionCertificateComponentViewModel : Screen
    {
        private readonly ICertificateProvider certificateProvider;
        private readonly IViewModelFactory<X509Certificate2ViewModel, X509Certificate2> certificate2ViewModelFactory;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<EncryptionCertificateComponentViewModel> logger;
        private readonly ICertificatePermissionProvider certPermissionProvider;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly PasswordPolicyOptions passwordOptions;

        public EncryptionCertificateComponentViewModel(INotifyModelChangedEventPublisher eventPublisher, ICertificateProvider certificateProvider, IViewModelFactory<X509Certificate2ViewModel, X509Certificate2> certificate2ViewModelFactory, IDialogCoordinator dialogCoordinator, ILogger<EncryptionCertificateComponentViewModel> logger, ICertificatePermissionProvider certPermissionProvider, DataProtectionOptions dataProtectionOptions, PasswordPolicyOptions passwordOptions)
        {
            this.eventPublisher = eventPublisher;
            this.certificateProvider = certificateProvider;
            this.certificate2ViewModelFactory = certificate2ViewModelFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.certPermissionProvider = certPermissionProvider;
            this.dataProtectionOptions = dataProtectionOptions;
            this.passwordOptions = passwordOptions;

            this.AvailableCertificates = new BindableCollection<X509Certificate2ViewModel>();
            eventPublisher.Register(this);
        }

        protected override void OnViewLoaded()
        {
            Task.Run(async () =>
            {
                await this.RefreshAvailableCertificates();
            });
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

        [NotifyModelChangedProperty]
        [AlsoNotifyFor(nameof(CanSetActiveCertificate))]
        public string ActiveCertificateThumbprint
        {
            get => this.passwordOptions.EncryptionCertificateThumbprint; set => this.passwordOptions.EncryptionCertificateThumbprint = value;
        }

        public bool CanSetActiveCertificate => !this.SelectedCertificate?.IsPublished ?? false;

        public async Task SetActiveCertificate()
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not set active certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not set active certificate\r\n{ex.Message}");
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
                logger.LogError(EventIDs.UIGenericError, ex, "Could not repermission certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not repermission the certificate\r\n{ex.Message}");
            }
        }

        public bool CanShowCertificateDialog => this.SelectedCertificate != null;

        public async Task ShowCertificateDialog()
        {
            try
            {
                var cert = this.SelectedCertificate?.Model;

                if (cert != null)
                {
                    X509Certificate2UI.DisplayCertificate(cert, this.GetHandle());
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not show certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not show the certificate\r\n{ex.Message}");
            }
        }


        public async Task OnListViewDoubleClick(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)(e.OriginalSource)).DataContext is X509Certificate2ViewModel))
            {
                return;
            }

            await this.ShowCertificateDialog();
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
                logger.LogError(EventIDs.UIGenericError, ex, "Could not export certificate");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not export the certificate\r\n{ex.Message}");
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
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not load certificate list");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not refresh the certificate list\r\n{ex.Message}");
            }
        }
    }
}
