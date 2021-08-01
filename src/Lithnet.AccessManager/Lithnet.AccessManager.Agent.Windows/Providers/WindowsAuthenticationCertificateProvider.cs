using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsAuthenticationCertificateProvider : AuthenticationCertificateProvider
    {
        private readonly IAadJoinInformationProvider aadProvider;

        public WindowsAuthenticationCertificateProvider(ILogger<AuthenticationCertificateProvider> logger, IAgentSettings settings, IAadJoinInformationProvider aadProvider)
        : base(logger, settings, StoreLocation.LocalMachine)
        {
            this.aadProvider = aadProvider;
        }

        public override T DelegateCertificateOperation<T>(Func<X509Certificate2, T> certificateOperation)
        {
            if (!this.settings.HasRegisteredSecondaryCredentials && this.settings.AuthenticationMode == AgentAuthenticationMode.Aad)
            {
                return this.aadProvider.DelegateCertificateOperation(certificateOperation);
            }

            return base.DelegateCertificateOperation(certificateOperation);
        }

        public override X509Certificate2 CreateSelfSignedCert()
        {
            return base.CreateSelfSignedCert();
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

                X509Store store = new X509Store(StoreName.My, this.storeLocation);
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();

                return cert;
            }
        }
    }
}
