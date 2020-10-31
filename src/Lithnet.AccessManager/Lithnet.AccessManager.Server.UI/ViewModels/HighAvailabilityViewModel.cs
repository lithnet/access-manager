using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Stylet;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Server.UI
{
    public class HighAvailabilityViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILicenseManager licenseManager;
        private readonly ILogger<HighAvailabilityViewModel> logger;
        private readonly ICertificateProvider certificateProvider;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly HighAvailabilityOptions highAvailabilityOptions;
        private readonly ICertificatePermissionProvider certPermissionProvider;
        private readonly EmailOptions emailOptions;
        private readonly AuthenticationOptions authnOptions;
        private readonly IProtectedSecretProvider secretProvider;
        private readonly IRegistryProvider registryProvider;
        private readonly ILicenseDataProvider licenseProvider;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly ICertificateSynchronizationProvider certSyncProvider;

        public HighAvailabilityViewModel(IDialogCoordinator dialogCoordinator, IShellExecuteProvider shellExecuteProvider, ILicenseManager licenseManager, ILogger<HighAvailabilityViewModel> logger, INotifyModelChangedEventPublisher eventPublisher, ICertificateProvider certificateProvider, IWindowsServiceProvider windowsServiceProvider, HighAvailabilityOptions highAvailabilityOptions, ICertificatePermissionProvider certPermissionProvider, EmailOptions emailOptions, AuthenticationOptions authnOptions, IProtectedSecretProvider secretProvider, IRegistryProvider registryProvider, ILicenseDataProvider licenseProvider, DataProtectionOptions dataProtectionOptions, ICertificateSynchronizationProvider certSyncProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
            this.logger = logger;
            this.certificateProvider = certificateProvider;
            this.windowsServiceProvider = windowsServiceProvider;
            this.highAvailabilityOptions = highAvailabilityOptions;
            this.certPermissionProvider = certPermissionProvider;
            this.emailOptions = emailOptions;
            this.authnOptions = authnOptions;
            this.secretProvider = secretProvider;
            this.registryProvider = registryProvider;
            this.licenseProvider = licenseProvider;
            this.dataProtectionOptions = dataProtectionOptions;
            this.certSyncProvider = certSyncProvider;
            this.dialogCoordinator = dialogCoordinator;

            this.licenseProvider.OnLicenseDataChanged += delegate
            {
                this.NotifyOfPropertyChange(nameof(this.IsEnterpriseEdition));
                this.NotifyOfPropertyChange(nameof(this.ShowEnterpriseEditionBanner));
            };

            this.DisplayName = "High availability";
            eventPublisher.Register(this);
            _ = this.RefreshAvailableCertificates();
        }

        public string HelpLink => Constants.HelpLinkPageBitLocker;

        public PackIconMaterialKind Icon => PackIconMaterialKind.Server;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink); // TODO Update link
        }

        public void LinkHaLearnMore()
        {
            // TODO Add link
        }

        public bool IsEnterpriseEdition => this.licenseManager.IsEnterpriseEdition();

        public bool ShowEnterpriseEditionBanner => this.licenseManager.IsEvaulatingOrBuiltIn() || !this.licenseManager.IsEnterpriseEdition();

        [NotifyModelChangedProperty]
        public string ClusterEncryptionKey
        {
            get => this.registryProvider.ServiceKeyThumbprint;
            set => this.registryProvider.ServiceKeyThumbprint = value;
        }

        public bool IsClusterEncryptionKeyPresent { get; set; }

        public bool IsClusterEncryptionKeyMissing { get; set; }

        [NotifyModelChangedProperty]
        [AlsoNotifyFor(nameof(UseSqlServer))]
        public bool UseLocalDB
        {
            get => !this.highAvailabilityOptions.UseExternalSql;
            set => this.highAvailabilityOptions.UseExternalSql = !value;
        }

        [AlsoNotifyFor(nameof(UseLocalDB))]
        [NotifyModelChangedProperty]
        public bool UseSqlServer
        {
            get => this.highAvailabilityOptions.UseExternalSql;
            set => this.highAvailabilityOptions.UseExternalSql = value;
        }

        [NotifyModelChangedProperty]
        public string ConnectionString
        {
            get => this.highAvailabilityOptions.DbConnectionString;
            set => this.highAvailabilityOptions.DbConnectionString = value;
        }

        public bool CanTestConnectionString => this.UseSqlServer && !string.IsNullOrWhiteSpace(this.ConnectionString);

        public void TestConnectionString()
        {

        }

        public async Task ClusterEncryptionKeyGenerate()
        {
            if (this.Certificate != null)
            {
                if (this.dialogCoordinator.ShowModalMessageExternal(this, "Confirm", "Are you sure you want to create a new cluster encryption key?", MessageDialogStyle.AffirmativeAndNegative) != MessageDialogResult.Affirmative)
                {
                    return;
                }
            }

            try
            {
                X509Certificate2 cert = this.certificateProvider.CreateSelfSignedCert("Lithnet Access Manager cluster encryption", CertificateProvider.LithnetAccessManagerClusterEncryptionEku);

                if (!await this.TryChangeOverCertificate(cert, true))
                {
                    return;
                }

                this.dialogCoordinator.ShowModalMessageExternal(this, "Next steps", "The new encryption key was successfully configured. Please export this certificate and import it into all cluster nodes");

                // TODO contact cluster nodes and send the key
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not generate encryption key");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not generate the key\r\n{ex.Message}");
            }
        }

        private async Task<bool> TryChangeOverCertificate(X509Certificate2 cert, bool addtoStore)
        {
            ProtectedSecret oldEmailOptionsPassword = this.emailOptions.Password;
            ProtectedSecret oldOidcSecret = this.authnOptions.Oidc?.Secret;
            X509Certificate2 oldCertificate = this.Certificate;
            string oldKey = this.ClusterEncryptionKey;
            bool hasAddedToStore = false;

            try
            {
                if (await this.TryReKeySecretsAsync(cert))
                {
                    if (addtoStore)
                    {
                        using X509Store store = X509ServiceStoreHelper.Open(AccessManager.Constants.ServiceName, OpenFlags.ReadWrite);
                        store.Add(cert);
                        hasAddedToStore = true;
                    }

                    this.certPermissionProvider.AddReadPermission(cert, this.windowsServiceProvider.GetServiceAccount());
                    this.Certificate = cert;
                    this.ClusterEncryptionKey = cert.Thumbprint;

                    return true;
                }
            }
            catch
            {
                if (oldOidcSecret != null)
                {
                    this.authnOptions.Oidc.Secret = oldOidcSecret;
                }

                this.emailOptions.Password = oldEmailOptionsPassword;

                this.Certificate = oldCertificate;
                this.ClusterEncryptionKey = oldKey;

                if (hasAddedToStore)
                {
                    using X509Store store = X509ServiceStoreHelper.Open(AccessManager.Constants.ServiceName, OpenFlags.ReadWrite);
                    store.Remove(cert);
                }

                throw;
            }

            return false;
        }

        private async Task<bool> TryReKeySecretsAsync(X509Certificate2 cert)
        {
            ProtectedSecret oldEmailOptionsPassword = this.emailOptions.Password;
            ProtectedSecret oldOidcSecret = this.authnOptions.Oidc?.Secret;

            if (this.emailOptions.Password != null)
            {
                ProtectedSecret response = await this.TryReKeySecretsAsync(this.emailOptions.Password, "SMTP password", cert);

                if (response == null)
                {
                    this.emailOptions.Password = oldEmailOptionsPassword;

                    return false;
                }

                this.emailOptions.Password = response;
            }

            if (this.authnOptions.Oidc?.Secret != null)
            {
                ProtectedSecret response = await this.TryReKeySecretsAsync(this.authnOptions.Oidc.Secret, "OpenID Connect secret", cert);

                if (response == null)
                {
                    this.emailOptions.Password = oldEmailOptionsPassword;
                    this.authnOptions.Oidc.Secret = oldOidcSecret;

                    return false;
                }

                this.authnOptions.Oidc.Secret = response;
            }

            return true;

        }

        private async Task<ProtectedSecret> TryReKeySecretsAsync(ProtectedSecret secret, string name, X509Certificate2 cert)
        {
            try
            {
                string rawEmail = this.secretProvider.UnprotectSecret(secret);

                return this.secretProvider.ProtectSecret(rawEmail);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, $"Unable to re-encrypt {name}");

                LoginDialogData data = await this.dialogCoordinator.ShowLoginAsync(this, "Error", $"The {name} could not be automatically re-encrypted. Please re-enter the {name}",
                                                                                   new LoginDialogSettings
                                                                                   {
                                                                                       ShouldHideUsername = true,
                                                                                       RememberCheckBoxVisibility = System.Windows.Visibility.Hidden,
                                                                                       AffirmativeButtonText = "OK",
                                                                                       NegativeButtonVisibility = System.Windows.Visibility.Visible,
                                                                                       NegativeButtonText = "Cancel"
                                                                                   });

                if (data != null)
                {
                    return this.secretProvider.ProtectSecret(data.Password, cert);
                }
            }

            return null;
        }

        private async Task RefreshAvailableCertificates()
        {
            this.IsClusterEncryptionKeyMissing = false;
            this.IsClusterEncryptionKeyPresent = false;

            if (this.ClusterEncryptionKey == null)
            {
                return;
            }

            try
            {
                IEnumerable<X509Certificate2> allCertificates = certificateProvider.GetEligibleClusterEncryptionCertificates(true).OfType<X509Certificate2>();

                foreach (X509Certificate2 certificate in allCertificates)
                {
                    if (string.Equals(certificate.Thumbprint, this.ClusterEncryptionKey, StringComparison.OrdinalIgnoreCase))
                    {
                        this.IsClusterEncryptionKeyPresent = true;
                        this.Certificate = certificate;

                        return;
                    }
                }

                this.IsClusterEncryptionKeyMissing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not load certificate list");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not refresh the certificate list\r\n{ex.Message}");
            }
        }

        private X509Certificate2 Certificate { get; set; }

        public bool CanShowCertificateDialog => this.Certificate != null;

        public void ShowCertificateDialog()
        {
            X509Certificate2UI.DisplayCertificate(this.Certificate, this.GetHandle());
        }

        public bool CanClusterEncryptionKeyExport => this.Certificate != null;

        public void ClusterEncryptionKeyExport()
        {
            NativeMethods.ShowCertificateExportDialog(this.GetHandle(), "Export key", this.Certificate);
        }

        public async Task ClusterEncryptionKeyImport()
        {
            try
            {
                using (X509Store store = X509ServiceStoreHelper.Open(AccessManager.Constants.ServiceName, OpenFlags.ReadWrite))
                {
                    X509Certificate2 newCert = NativeMethods.ShowCertificateImportDialog(this.GetHandle(), "Import cluster key", store);

                    if (newCert != null)
                    {
                        if (!await this.TryChangeOverCertificate(newCert, false))
                        {
                            store.Remove(newCert);

                            return;
                        }

                        this.dialogCoordinator.ShowModalMessageExternal(this, "Import cluster key", "The encryption key was successfully imported. Please ensure you import this certificate on all cluster nodes");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not import key");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not import the key\r\n{ex.Message}");
            }
        }

        public async Task SynchronizeSecrets()
        {
            try
            {
                this.certSyncProvider.ExportCertificatesToConfig();
                this.certSyncProvider.ImportCertificatesFromConfig();
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Could not synchronize secrets");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to complete the synchronization process\r\n{ex.Message}");
            }
        }
    }
}