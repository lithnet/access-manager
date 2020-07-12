using System;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Lithnet.AccessManager.Interop;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager
{
    public class CertificateProvider : ICertificateProvider
    {
        private readonly IDirectory directory;

        private readonly ILogger logger;

        private readonly IAppPathProvider appPathProvider;

        public const string LithnetAccessManagerEku = "1.3.6.1.4.1.55989.2.1.1";

        public X509Certificate2 CreateSelfSignedCert(string subject)
        {
            CertificateRequest request = new CertificateRequest($"CN={subject},OU=Access Manager,O=Lithnet", RSA.Create(4096), HashAlgorithmName.SHA384, RSASignaturePadding.Pss);

            var enhancedKeyUsage = new OidCollection();
            enhancedKeyUsage.Add(new Oid(LithnetAccessManagerEku, "Lithnet Access Manager Encryption"));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, critical: true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));

            X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTime.UtcNow.AddYears(20));
            string p = Guid.NewGuid().ToString();
            var raw = cert.Export(X509ContentType.Pfx, p);
            var f = new X509Certificate2(raw, p, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            return f;
        }

        public CertificateProvider(ILogger<CertificateProvider> logger, IDirectory directory, IAppPathProvider appPathProvider)
        {
            this.directory = directory;
            this.logger = logger;
            this.appPathProvider = appPathProvider;
        }

        public X509Certificate2 GetCertificateWithPrivateKey(string thumbprint)
        {
            return this.FindCertificate(true, thumbprint, null);
        }

        public X509Certificate2 FindCertificate(bool requirePrivateKey, string thumbprint = null, string pathHint = null)
        {
            X509Certificate2 cert = null;

            if (!string.IsNullOrWhiteSpace(thumbprint))
            {
                cert = this.ResolveCertificateFromLocalStore(thumbprint);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(pathHint))
                {
                    this.TryGetCertificateFromPath(pathHint, out cert);
                }
            }

            if (cert == null)
            {
                this.TryGetCertificateFromDirectory(out cert, WindowsIdentity.GetCurrent().User);
            }

            if (requirePrivateKey && !cert.HasPrivateKey)
            {
                throw new CertificateValidationException($"The certificate was found, but the private key was not available. Thumbprint: {thumbprint}");
            }

            if (cert == null)
            {
                throw new CertificateNotFoundException($"A certificate could not be found. Requested thumbprint {thumbprint}. Path hint: {pathHint}");
            }

            return cert;
        }

        public X509Certificate2Collection GetEligibleCertificates(bool needPrivateKey)
        {
            X509Certificate2Collection certs = new X509Certificate2Collection();

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine, OpenFlags.ReadOnly);
            Oid serverAuthOid = new Oid(LithnetAccessManagerEku);

            foreach (X509Certificate2 c in store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false).OfType<X509Certificate2>().Where(t => !needPrivateKey || t.HasPrivateKey))
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

        public X509Certificate2 GetCertificateFromDirectory(SecurityIdentifier domainSid)
        {
            string dnsDomain = NativeMethods.GetDnsDomainNameFromSid(domainSid.AccountDomainSid);
            return GetCertificateFromDirectory(dnsDomain);
        }

        public X509Certificate2 GetCertificateFromDirectory(string dnsDomain)
        {
            var cnc = this.directory.GetConfigurationNamingContext(dnsDomain);
            string dn = cnc.GetPropertyString("distinguishedName");
            dn = $"LDAP://CN=AccessManagerPublicKey,CN=Lithnet,CN=Services,{dn}";
            DirectoryEntry amobject = new DirectoryEntry(dn);

            byte[] data = amobject?.GetPropertyBytes("msDS-ByteArray");

            if (data != null)
            {
                return new X509Certificate2(data);
            }

            throw new ObjectNotFoundException("There was no certificate published in the directory");
        }

        public void PublishCertificateToDirectory(X509Certificate2 cert, string dnsDomain)
        {
            
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

        public bool TryGetCertificateFromDirectory(out X509Certificate2 cert, SecurityIdentifier domain)
        {
            cert = null;

            try
            {
                cert = this.GetCertificateFromDirectory(domain);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "TryGetCertificateFromDirectory failed");
            }

            return false;
        }

        internal bool TryGetCertificateFromPath(string path, out X509Certificate2 cert)
        {
            cert = null;

            if (Uri.TryCreate(path, UriKind.Absolute, out Uri u))
            {
                if (u.IsFile || u.IsUnc)
                {
                    return this.TryGetCertificateFromFile(path, out cert);
                }
                else
                {
                    return this.TryGetCertificateFromUrl(u, out cert);
                }
            }
            else if (Uri.TryCreate(path, UriKind.Relative, out Uri p))
            {
                var testPath = Path.Combine(this.appPathProvider.AppPath, path);
                return this.TryGetCertificateFromFile(testPath, out cert);
            }

            return false;
        }

        internal bool TryGetCertificateFromUrl(Uri path, out X509Certificate2 cert)
        {
            cert = null;

            try
            {
                if (path.Scheme != "https")
                {
                    logger.LogError(EventIDs.UnsupportedUriScheme, "Can not obtain certificate from URL, as only https URLs are supported: {path}", path);
                    return false;
                }

                HttpClient client = new HttpClient();
                var result = client.GetAsync(path).ConfigureAwait(false).GetAwaiter().GetResult();

                result.EnsureSuccessStatusCode();

                byte[] data = result.Content.ReadAsByteArrayAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                cert = new X509Certificate2(data);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "TryGetCertificateFromUrl failed");
            }

            return false;
        }

        internal bool TryGetCertificateFromFile(string path, out X509Certificate2 cert)
        {
            cert = null;

            try
            {
                cert = new X509Certificate2(path);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "TryGetCertificateFromFile failed");
            }

            return false;
        }

        internal bool TryGetCertificateFromThumbprint(string thumbprint, out X509Certificate2 cert)
        {
            try
            {
                cert = this.ResolveCertificateFromLocalStore(thumbprint);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogTrace(ex, "TryGetCertificateFromThumbprint failed");

                cert = null;
                return false;
            }
        }

        private X509Certificate2 ResolveCertificateFromLocalStore(string thumbprint)
        {
            return GetCertificateFromStore(thumbprint, StoreLocation.CurrentUser) ??
                    GetCertificateFromStore(thumbprint, StoreLocation.LocalMachine) ??
                    throw new CertificateNotFoundException($"A certificate with the thumbprint {thumbprint} could not be found");
        }

        private X509Certificate2 GetCertificateFromStore(string thumbprint, StoreLocation storeLocation)
        {
            X509Store store = new X509Store(StoreName.My, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            try
            {
                foreach (var item in store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false))
                {
                    return item;
                }
            }
            finally
            {
                if (store.IsOpen)
                {
                    store.Close();
                }
            }

            return null;
        }
    }
}
