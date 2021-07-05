using Lithnet.AccessManager.Agent.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent
{
    public class AuthenticationCertificateProvider : IAuthenticationCertificateProvider
    {
        private readonly IAgentSettings settings;
        private readonly IAadJoinInformationProvider aadProvider;
        private readonly ILogger<AuthenticationCertificateProvider> logger;

        private X509Certificate2 cachedCertificate;

        public AuthenticationCertificateProvider(IAgentSettings settings, IAadJoinInformationProvider aadProvider, ILogger<AuthenticationCertificateProvider> logger)
        {
            this.settings = settings;
            this.aadProvider = aadProvider;
            this.logger = logger;
        }

        public T DelegateCertificateOperation<T>(Func<X509Certificate2, T> certificateOperation)
        {
            if (this.settings.AuthenticationMode == AgentAuthenticationMode.Ams || this.settings.HasRegisteredSecondaryCredentials)
            {
                if (string.IsNullOrWhiteSpace(this.settings.AuthCertificate))
                {
                    throw new CertificateNotFoundException("The authentication certificate thumbprint was not specified in the application settings");
                }

                var cert = this.ResolveCertificateFromLocalStore(this.settings.AuthCertificate);

                this.logger.LogTrace($"Found AMS certificate with thumbprint {cert.Thumbprint}");
                return certificateOperation(cert);
            }
            else if (this.settings.AuthenticationMode == AgentAuthenticationMode.Aad)
            {
                return this.aadProvider.DelegateCertificateOperation(certificateOperation);
            }

            throw new InvalidOperationException("The authentication mode is not supported");
        }

        public Task<X509Certificate2> GetOrCreateAgentCertificate()
        {
            X509Certificate2 cert;

            if (!string.IsNullOrWhiteSpace(this.settings.AuthCertificate))
            {
                if (this.cachedCertificate != null)
                {
                    if (string.Equals(this.cachedCertificate.Thumbprint, this.settings.AuthCertificate, StringComparison.OrdinalIgnoreCase))
                    {
                        return Task.FromResult(this.cachedCertificate);
                    }
                }

                try
                {
                    this.cachedCertificate = this.ResolveCertificateFromLocalStore(this.settings.AuthCertificate);
                    this.logger.LogTrace($"Found authentication certificate {this.settings.AuthCertificate} in the local store");
                    return Task.FromResult(this.cachedCertificate);
                }
                catch (CertificateNotFoundException)
                {
                }
            }

            cert = this.GetCertificateFromStoreByOid(Constants.AgentAuthenticationCertificateOid, StoreLocation.LocalMachine);

            if (cert != null)
            {
                this.settings.AuthCertificate = cert.Thumbprint;
                this.cachedCertificate = cert;
                this.logger.LogTrace($"Found an existing, but unattached certificate {this.settings.AuthCertificate} in the local store");

                return Task.FromResult(this.cachedCertificate);
            }

            cert = this.CreateSelfSignedCert();
            this.cachedCertificate = cert;
            this.settings.AuthCertificate = cert.Thumbprint;

            this.logger.LogTrace($"Created a new authentication certificate {this.settings.AuthCertificate}");
            return Task.FromResult(cert);
        }

        public void DeleteAgentCertificates()
        {
            this.RemoveCertificatesFromStoreByOid(Constants.AgentAuthenticationCertificateOid, StoreLocation.LocalMachine);
        }

        public X509Certificate2 CreateSelfSignedCert()
        {
            CertificateRequest request = new CertificateRequest($"CN={Environment.MachineName},OU=Agent,OU=Access Manager,O=Lithnet", RSA.Create(3072), HashAlgorithmName.SHA384, RSASignaturePadding.Pss);

            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
            {
                new Oid(Constants.AgentAuthenticationCertificateOid, "Access Manager Agent Authentication")
            }, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature |
                X509KeyUsageFlags.NonRepudiation
                , true));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

            X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTime.UtcNow.AddYears(10));
            string p = Guid.NewGuid().ToString();
            var raw = cert.Export(X509ContentType.Pfx, p);
            var f = new X509Certificate2(raw, p, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(f);
            store.Close();

            return f;
        }

        private X509Certificate2 CreateSelfSignedCertInTpm()
        {
            RSA rsa;

            // We might need to do something like this to get around the internal error bug with the platform provider
            // https://github.com/glueckkanja-pki/TPMImport

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    KeyCreationOptions = CngKeyCreationOptions.MachineKey,
                    KeyUsage = CngKeyUsages.AllUsages,
                    Provider = new CngProvider("Microsoft Platform Crypto Provider"),
                    ExportPolicy = CngExportPolicies.None,
                    UIPolicy = new CngUIPolicy(CngUIProtectionLevels.None),
                };

                // creationParameters.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(1024 * 3), CngPropertyOptions.None));

                CngKey key = CngKey.Create(CngAlgorithm.Rsa, Guid.NewGuid().ToString(), creationParameters);
                rsa = new RSACng(key);
            }
            else
            {
                rsa = RSA.Create(3 * 1024);
            }

            using (rsa)
            {
                CertificateRequest request = new CertificateRequest($"CN={Environment.MachineName},OU=Agent,OU=Access Manager,O=Lithnet", rsa, HashAlgorithmName.SHA384, RSASignaturePadding.Pss);

                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
                {
                    new Oid(Constants.AgentAuthenticationCertificateOid, "Access Manager Agent Authentication")
                }, true));
                request.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyEncipherment |
                    X509KeyUsageFlags.DigitalSignature |
                    X509KeyUsageFlags.NonRepudiation
                    , true));
                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

                X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTime.UtcNow.AddYears(10));

                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();

                return cert;
            }
        }

        private X509Certificate2 GetCertificateFromStoreByOid(string oid, StoreLocation storeLocation)
        {
            using (X509Store store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (var item in store.Certificates.Find(X509FindType.FindByApplicationPolicy, oid, false))
                {
                    foreach (var extension in item.Extensions.OfType<X509EnhancedKeyUsageExtension>())
                    {
                        foreach (Oid o in extension.EnhancedKeyUsages)
                        {
                            if (o.Value == oid)
                            {
                                return item;
                            }
                        }
                    }
                }

                return null;
            }
        }

        private void RemoveCertificatesFromStoreByOid(string oid, StoreLocation storeLocation)
        {
            using (X509Store store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                foreach (var item in store.Certificates.Find(X509FindType.FindByApplicationPolicy, oid, false))
                {
                    foreach (var extension in item.Extensions.OfType<X509EnhancedKeyUsageExtension>())
                    {
                        foreach (Oid o in extension.EnhancedKeyUsages)
                        {
                            if (o.Value == oid)
                            {
                                store.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        private X509Certificate2 ResolveCertificateFromLocalStore(string thumbprint)
        {
            return GetCertificateFromStore(thumbprint, StoreLocation.CurrentUser) ??
                   GetCertificateFromStore(thumbprint, StoreLocation.LocalMachine) ??
                   throw new CertificateNotFoundException($"An authentication certificate with the thumbprint {thumbprint} could not be found");
        }

        private X509Certificate2 GetCertificateFromStore(string thumbprint, StoreLocation storeLocation)
        {
            using (X509Store store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (var item in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false))
                {
                    return item;
                }

                return null;
            }
        }
    }
}
