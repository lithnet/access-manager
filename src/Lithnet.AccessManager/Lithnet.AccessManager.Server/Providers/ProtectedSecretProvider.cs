using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server
{
    public class ProtectedSecretProvider : IProtectedSecretProvider
    {
        private readonly IEncryptionProvider encryptionProvider;
        private readonly ICertificateProvider certificateProvider;
        private readonly RandomNumberGenerator rng;
        private readonly IProductSettingsProvider productSettings;

        public ProtectedSecretProvider(IEncryptionProvider encryptionProvider, ICertificateProvider certificateProvider, RandomNumberGenerator rng, IClusterProvider clusterProvider, IProductSettingsProvider productSettings)
        {
            this.encryptionProvider = encryptionProvider;
            this.certificateProvider = certificateProvider;
            this.rng = rng;
            this.productSettings = productSettings;
        }

        public string UnprotectSecret(ProtectedSecret data)
        {
            try
            {
                if (data?.Data == null)
                {
                    return null;
                }

                if (data.Mode == 0)
                {
                    return data.Data;
                }

                if (data.Mode == 1)
                {
                    return this.UnprotectSecretv1(data);
                }

                if (data.Mode == 2)
                {
                    return this.UnprotectSecretv2(data);
                }

                throw new ConfigurationException("The data was protected with an encryption mechanism not known to this version of the application");
            }
            catch (Exception ex)
            {
                throw new ConfigurationException("Unable to decrypt the encrypted data from the configuration file. Use the configuration manager application to re-enter the secret, and try again", ex);
            }
        }

        private string UnprotectSecretv1(ProtectedSecret data)
        {
            byte[] salt = Convert.FromBase64String(data.Salt);
            byte[] protectedData = Convert.FromBase64String(data.Data);
            byte[] unprotectedData = ProtectedData.Unprotect(protectedData, salt, DataProtectionScope.LocalMachine);

            return Encoding.UTF8.GetString(unprotectedData);
        }

        private string UnprotectSecretv2(ProtectedSecret data)
        {
            return this.encryptionProvider.Decrypt(data.Data, this.certificateProvider.FindDecryptionCertificate);
        }

        public ProtectedSecret ProtectSecret(string secret)
        {
            var thumbprint = this.productSettings.GetEncryptionCertificateThumbprint();

            if (thumbprint == null)
            {
                return this.ProtectSecretv1(secret);
            }
            else
            {
                return this.ProtectSecretv2(secret, thumbprint);
            }
        }

        public ProtectedSecret ProtectSecret(string secret, X509Certificate2 cert)
        {
            ProtectedSecret protectedData = new ProtectedSecret
            {
                Mode = 2,
                Data = encryptionProvider.Encrypt(cert, secret)
            };

            return protectedData;
        }

        private ProtectedSecret ProtectSecretv2(string secret, string thumbprint)
        {
            var cert = certificateProvider.FindEncryptionCertificate(thumbprint);
            return this.ProtectSecret(secret, cert);
        }

        private ProtectedSecret ProtectSecretv1(string secret)
        {
            byte[] salt = new byte[128];
            this.rng.GetBytes(salt);

            ProtectedSecret protectedData = new ProtectedSecret
            {
                Mode = 1,
                Salt = Convert.ToBase64String(salt),
                Data = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), salt, DataProtectionScope.LocalMachine))
            };

            return protectedData;
        }
    }
}
