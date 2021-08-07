using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class MacOsAuthenticationCertificateProvider : AuthenticationCertificateProvider
    {
        public MacOsAuthenticationCertificateProvider(ILogger<AuthenticationCertificateProvider> logger, IAgentSettings settings)
            : base(logger, settings, StoreLocation.LocalMachine)
        {
        }

        public override X509Certificate2 CreateSelfSignedCert()
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
            var f = new X509Certificate2(raw, p, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable); // Persist flag must not be set for macos

            X509Store store = new X509Store(StoreName.My, this.storeLocation);
            store.Open(OpenFlags.ReadWrite);
            store.Add(f);
            store.Close();

            return f;
        }
    }
}
