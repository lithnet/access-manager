using Lithnet.AccessManager.Agent.Providers;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Agent
{
    public class AuthenticationCertificateProvider : IAuthenticationCertificateProvider
    {
        private readonly ISettingsProvider settings;

        public AuthenticationCertificateProvider(ISettingsProvider settings)
        {
            this.settings = settings;
        }

        public X509Certificate2 GetCertificate()
        {
            if (this.settings.AuthenticationMode == AuthenticationMode.Ssa)
            {
                X509Certificate2 cert = this.GetCertificateFromStoreByOid(Constants.AgentAuthenticationCertificateOid, StoreLocation.CurrentUser);

                if (cert == null)
                {
                    throw new CertificateNotFoundException("No valid certificate could be found in the store");
                }

                return cert;
            }

            if (this.settings.AuthenticationMode == AuthenticationMode.Aadj)
            {
                return this.GetAadJoinCertificate();
            }

            throw new InvalidOperationException("The authentication mode is not supported");
        }

        private X509Certificate2 GetAadJoinCertificate()
        {
            NetApi32.NetGetAadJoinInformation(null, out NetApi32.DSREG_JOIN_INFO joinInfo).ThrowIfFailed();

            if (joinInfo == null)
            {
                throw new InvalidOperationException("The machine was not joined to an Azure Active Directory");
            }

            if (!joinInfo.pJoinCertificate.HasValue)
            {
                throw new CertificateNotFoundException("The AAD join information was found, but a certificate was not present");
            }

            byte[] data = new byte[joinInfo.pJoinCertificate.Value.cbCertEncoded];
            Marshal.Copy(joinInfo.pJoinCertificate.Value.pbCertEncoded, data, 0, data.Length);

            var tcert = new X509Certificate2(data);

            return this.ResolveCertificateFromLocalStore(tcert.Thumbprint);
        }

        private X509Certificate2 GetCertificateFromStoreByOid(string oid, StoreLocation storeLocation)
        {
            using (X509Store store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (var item in store.Certificates.Find(X509FindType.FindByExtension, oid, false))
                {
                    return item;
                }

                return null;
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
