using System;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public class CertificateProvider : ICertificateProvider
    {
        private static readonly bool isService = GetServiceStatus();

        private readonly ILogger logger;

        private readonly IDiscoveryServices discoveryServices;

        public static Oid LithnetAccessManagerAdPasswordEncryptionEku = new Oid("1.3.6.1.4.1.55989.2.1.1", "Lithnet Access Manager password encryption (AD)");
        public static Oid LithnetAccessManagerAmsPasswordEncryptionEku = new Oid("1.3.6.1.4.1.55989.2.1.4", "Lithnet Access Manager password encryption (AMS)");
        public static Oid ServerAuthenticationEku = new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication");

        public X509Certificate2 CreateSelfSignedCert(string subject, Oid eku)
        {
            CertificateRequest request = new CertificateRequest($"CN={subject},OU=Access Manager,O=Lithnet", RSA.Create(4096), HashAlgorithmName.SHA384, RSASignaturePadding.Pss);

            if (eku != null)
            {
                var enhancedKeyUsage = new OidCollection { eku };
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, critical: true));
            }

            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

            X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTime.UtcNow.AddYears(20));
            string p = Guid.NewGuid().ToString();
            var raw = cert.Export(X509ContentType.Pfx, p);
            var f = new X509Certificate2(raw, p, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            return f;
        }

        public CertificateProvider(ILogger<CertificateProvider> logger, IDiscoveryServices discoveryServices)
        {
            this.logger = logger;
            this.discoveryServices = discoveryServices;
        }

        public X509Certificate2 FindDecryptionCertificate(string thumbprint)
        {
            var cert = this.ResolveCertificateFromLocalStore(thumbprint);

            if (cert == null)
            {
                throw new CertificateNotFoundException($"A certificate could not be found. Requested thumbprint {thumbprint}");
            }

            if (!cert.HasPrivateKey)
            {
                throw new CertificateValidationException($"The certificate was found, but the private key was not available. Thumbprint: {thumbprint}");
            }

            return cert;
        }


        public X509Certificate2 FindEncryptionCertificate()
        {
            return this.FindEncryptionCertificate(null);
        }

        public X509Certificate2 FindEncryptionCertificate(string thumbprint)
        {
            X509Certificate2 cert = null;

            if (!string.IsNullOrWhiteSpace(thumbprint))
            {
                cert = this.ResolveCertificateFromLocalStore(thumbprint);
            }

            if (cert == null)
            {
                this.TryGetCertificateFromDirectory(out X509Certificate2 directoryCert, Domain.GetComputerDomain().Name);
                if (thumbprint == null || directoryCert.Thumbprint == thumbprint)
                {
                    cert = directoryCert;
                }
            }

            if (cert == null)
            {
                throw new CertificateNotFoundException($"A certificate could not be found. Requested thumbprint {thumbprint}");
            }

            return cert;
        }

        public X509Certificate2Collection GetEligibleAdPasswordEncryptionCertificates(bool needPrivateKey)
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();

            X509Store store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadOnly);
            GetEligibleCertificates(needPrivateKey, LithnetAccessManagerAdPasswordEncryptionEku, store, certs);

            return certs;
        }

        public X509Certificate2Collection GetEligibleAmsPasswordEncryptionCertificates(bool needPrivateKey)
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();

            X509Store store = X509ServiceStoreHelper.Open(Constants.ServiceName, OpenFlags.ReadOnly);
            GetEligibleCertificates(needPrivateKey, LithnetAccessManagerAmsPasswordEncryptionEku, store, certs);

            return certs;
        }
        
        private static void GetEligibleCertificates(bool needPrivateKey, Oid eku, X509Store store, X509Certificate2Collection certs)
        {
            foreach (X509Certificate2 c in store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false)
                .OfType<X509Certificate2>().Where(t => !needPrivateKey || t.HasPrivateKey))
            {
                foreach (X509EnhancedKeyUsageExtension x in c.Extensions.OfType<X509EnhancedKeyUsageExtension>())
                {
                    foreach (Oid o in x.EnhancedKeyUsages)
                    {
                        if (o.Value == eku.Value)
                        {
                            certs.Add(c);
                        }
                    }
                }
            }
        }

        public X509Certificate2 GetCertificateFromDirectory(string dnsDomain)
        {
            var cnc = this.discoveryServices.GetConfigurationNamingContext(dnsDomain);
            string dn = cnc.GetPropertyString("distinguishedName");
            dn = $"LDAP://CN=AccessManagerConfig,CN=Lithnet,CN=Services,{dn}";
            DirectoryEntry amobject = new DirectoryEntry(dn);

            byte[] data = amobject?.GetPropertyBytes("caCertificate");

            if (data != null)
            {
                return new X509Certificate2(data);
            }

            throw new ObjectNotFoundException("There was no certificate published in the directory");
        }

        public X509Certificate2Collection GetEligibleServerAuthenticationCertificates()
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            GetEligibleCertificates(true, ServerAuthenticationEku, store, certs);

            return certs;
        }

        public bool TryGetCertificateFromDirectory(out X509Certificate2 cert, string dnsDomain)
        {
            cert = null;

            try
            {
                cert = this.GetCertificateFromDirectory(dnsDomain);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "TryGetCertificateFromDirectory failed");
            }

            return false;
        }

        private X509Certificate2 ResolveCertificateFromLocalStore(string thumbprint)
        {
            return GetCertificateFromServiceStore(thumbprint, Constants.ServiceName) ??
                   GetCertificateFromStore(thumbprint, StoreLocation.CurrentUser) ??
                   GetCertificateFromStore(thumbprint, StoreLocation.LocalMachine) ??
                   throw new CertificateNotFoundException($"A certificate with the thumbprint {thumbprint} could not be found");
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

        private static bool GetServiceStatus()
        {
            using (AuthorizationContext c = new AuthorizationContext(WindowsIdentity.GetCurrent().AccessToken))
            {
                return c.ContainsSid(new SecurityIdentifier("S-1-5-6"));
            }
        }

        private X509Certificate2 GetCertificateFromServiceStore(string thumbprint, string serviceName)
        {
            if (!isService && serviceName == null)
            {
                return null;
            }

            try
            {
                X509Store store;
                if (serviceName == null)
                {
                    store = X509ServiceStoreHelper.Open(OpenFlags.ReadOnly);
                }
                else
                {
                    store = X509ServiceStoreHelper.Open(serviceName, OpenFlags.ReadOnly);
                }

                using (store)
                {
                    foreach (var item in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false))
                    {
                        return item;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
