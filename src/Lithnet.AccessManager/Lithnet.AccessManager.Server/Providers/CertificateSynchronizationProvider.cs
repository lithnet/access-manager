using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Licensing.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server
{
    public class CertificateSynchronizationProvider : ICertificateSynchronizationProvider
    {
        private readonly RandomNumberGenerator rng;
        private readonly ILogger<CertificateSynchronizationProvider> logger;
        private DataProtectionOptions dataProtectionOptions;
        private readonly IProtectedSecretProvider secretProvider;
        private readonly ICertificatePermissionProvider certPermissionProvider;
        private readonly IAmsLicenseManager licenseManager;

        public CertificateSynchronizationProvider(RandomNumberGenerator rng, ILogger<CertificateSynchronizationProvider> logger, IOptionsMonitor<DataProtectionOptions> dataProtectionOptions, IProtectedSecretProvider secretProvider, ICertificatePermissionProvider certPermissionProvider, IAmsLicenseManager licenseManager)
            : this(rng, logger, secretProvider, certPermissionProvider, licenseManager)
        {
            this.dataProtectionOptions = dataProtectionOptions.CurrentValue;
            dataProtectionOptions.OnChange((o, s) =>
            {
                this.dataProtectionOptions = o;
                this.ImportCertificatesFromConfig();
            });
        }

        public CertificateSynchronizationProvider(RandomNumberGenerator rng, ILogger<CertificateSynchronizationProvider> logger, DataProtectionOptions dataProtectionOptions, IProtectedSecretProvider secretProvider, ICertificatePermissionProvider certPermissionProvider, IAmsLicenseManager licenseManager)
            : this(rng, logger, secretProvider, certPermissionProvider, licenseManager)
        {
            this.dataProtectionOptions = dataProtectionOptions;
        }

        private CertificateSynchronizationProvider(RandomNumberGenerator rng, ILogger<CertificateSynchronizationProvider> logger, IProtectedSecretProvider secretProvider, ICertificatePermissionProvider certPermissionProvider, IAmsLicenseManager licenseManager)
        {
            this.rng = rng;
            this.logger = logger;
            this.secretProvider = secretProvider;
            this.certPermissionProvider = certPermissionProvider;
            this.licenseManager = licenseManager;
        }

        public void ExportCertificatesToConfig()
        {
            if (!this.dataProtectionOptions.EnableCertificateSynchronization)
            {
                dataProtectionOptions.Certificates = null;
                return;
            }

            if (!this.licenseManager.IsFeatureEnabled(LicensedFeatures.CertificateSynchronization))
            {
                return;
            }

            this.dataProtectionOptions.Certificates ??= new List<CertificateData>();

            using (X509Store store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadOnly))
            {
                foreach (X509Certificate2 cert in store.Certificates.OfType<X509Certificate2>())
                {
                    if (!cert.HasPrivateKey)
                    {
                        logger.LogWarning(EventIDs.CertificateSynchronizationExportWarningNoPrivateKey, "Skipping synchronization of certificate {cert} as it does not have a private key", cert.Thumbprint);
                        continue;
                    }

                    this.ExportCertificateToConfig(cert);
                }
            }
        }

        private void ExportCertificateToConfig(X509Certificate2 cert)
        {
            if (!this.dataProtectionOptions.EnableCertificateSynchronization)
            {
                return;
            }

            this.licenseManager.ThrowOnMissingFeature(LicensedFeatures.CertificateSynchronization);

            try
            {
                var match = dataProtectionOptions.Certificates?.FirstOrDefault(t => string.Equals(t.Thumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    this.logger.LogTrace("Certificate with thumbprint {thumbprint} not found in the synchronization store and will be added", cert.Thumbprint);
                    dataProtectionOptions.Certificates.Add(this.CreateCertificateDataEntry(cert));
                }
                else
                {
                    var securityDescriptorFromSecret = this.secretProvider.GetSecurityDescriptorFromSecret(match.Secret);
                    var securityDescriptorTemplate = $"SDDL={this.secretProvider.BuildDefaultSecurityDescriptor().GetSddlForm(AccessControlSections.All)}";

                    if (!string.Equals(securityDescriptorFromSecret, securityDescriptorTemplate))
                    {
                        this.logger.LogTrace("Certificate {thumbprint} has a mismatched security descriptor and will be regenerated. Current: {currentSecurityDescriptor}, Expected: {expectedSecurityDescriptor}", cert.Thumbprint, securityDescriptorFromSecret, securityDescriptorTemplate);

                        var newEntry = this.CreateCertificateDataEntry(cert);
                        match.Data = newEntry.Data;
                        match.Secret = newEntry.Secret;
                    }
                    else
                    {
                        this.logger.LogTrace("Certificate {thumbprint} was up to date", cert.Thumbprint);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.CertificateSynchronizationExportError, ex, "Could not export certificate {thumbprint}", cert.Thumbprint);
            }
        }

        private CertificateData CreateCertificateDataEntry(X509Certificate2 cert)
        {
            string password = GenerateStrongPassword();

            CertificateData data = new CertificateData
            {
                Secret = this.secretProvider.ProtectSecret(password),
                Data = Convert.ToBase64String(cert.Export(X509ContentType.Pfx, password)),
                Thumbprint = cert.Thumbprint
            };

            return data;
        }

        private string GenerateStrongPassword()
        {
            byte[] passwordData = new byte[256];
            rng.GetBytes(passwordData);
            return Encoding.Unicode.GetString(passwordData);
        }

        public void ImportCertificatesFromConfig()
        {
            if (!this.dataProtectionOptions.EnableCertificateSynchronization)
            {
                return;
            }

            try
            {
                this.licenseManager.ThrowOnMissingFeature(LicensedFeatures.CertificateSynchronization);

                using (X509Store store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadWrite))
                {
                    foreach (var certData in dataProtectionOptions.Certificates)
                    {
                        if (certData.Operation != CertificateOperation.Add)
                        {
                            continue;
                        }

                        try
                        {
                            X509Certificate2 cert = store.Certificates.Find(X509FindType.FindByThumbprint, certData.Thumbprint, false).OfType<X509Certificate2>().FirstOrDefault();

                            if (cert == null || !cert.HasPrivateKey)
                            {
                                this.logger.LogTrace("The certificate {thumbprint} was not found in the local store and will be added", certData.Thumbprint);

                                string password = this.secretProvider.UnprotectSecret(certData.Secret);

                                cert = new X509Certificate2(Convert.FromBase64String(certData.Data), password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
                                store.Add(cert);
                                this.certPermissionProvider.AddReadPermission(cert);

                                logger.LogTrace("Added certificate {thumbprint} to the service store", certData.Thumbprint);
                            }
                            else
                            {
                                logger.LogTrace("Certificate {thumbprint} was already in the store", certData.Thumbprint);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(EventIDs.CertificateSynchronizationImportError, ex, "Could not import certificate {thumbprint}", certData.Thumbprint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.CertificateSynchronizationImportError, ex, "Error performing certificate import");
            }
        }
    }
}
