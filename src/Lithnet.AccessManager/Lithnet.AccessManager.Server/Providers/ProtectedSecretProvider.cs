using System;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Options;
using Vanara.InteropServices;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Server
{
    public class ProtectedSecretProvider : IProtectedSecretProvider
    {
        private const int ReadPublicAndPrivateKey = 0x3;
        private const int ReadPublicKey = 0x2;

        private readonly ICertificateProvider certificateProvider;
        private readonly DataProtectionOptions dataProtectionOptions;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly IAmsLicenseManager licenseManager;
        private readonly RandomNumberGenerator rng;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly IRegistryProvider registryProvider;


        public ProtectedSecretProvider(IEncryptionProvider encryptionProvider, ICertificateProvider certificateProvider, RandomNumberGenerator rng, IOptions<DataProtectionOptions> dataProtectionOptions, IWindowsServiceProvider windowsServiceProvider, IAmsLicenseManager licenseManager, IRegistryProvider registryProvider)
        {
            this.encryptionProvider = encryptionProvider;
            this.certificateProvider = certificateProvider;
            this.rng = rng;
            this.dataProtectionOptions = dataProtectionOptions.Value;
            this.windowsServiceProvider = windowsServiceProvider;
            this.licenseManager = licenseManager;
            this.registryProvider = registryProvider;
        }

        public CommonSecurityDescriptor BuildDefaultSecurityDescriptor()
        {
            var domainAdmins = new SecurityIdentifier(WellKnownSidType.AccountDomainAdminsSid, Domain.GetComputerDomain().GetDirectoryEntry().GetPropertySid("objectSid"));
            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, domainAdmins, null, null, new DiscretionaryAcl(false, false, 1));

            sd.DiscretionaryAcl.AddAccess(AccessControlType.Allow, this.windowsServiceProvider.GetServiceAccountSid(), ReadPublicAndPrivateKey, InheritanceFlags.None, PropagationFlags.None);

            if (!string.IsNullOrWhiteSpace(dataProtectionOptions.AuthorizedSecretReaders))
            {
                if (dataProtectionOptions.AuthorizedSecretReaders.TryParseAsSid(out SecurityIdentifier sid))
                {
                    sd.DiscretionaryAcl.AddAccess(AccessControlType.Allow, sid, ReadPublicAndPrivateKey, InheritanceFlags.None, PropagationFlags.None);
                }
            }

            sd.DiscretionaryAcl.AddAccess(AccessControlType.Allow, domainAdmins, ReadPublicAndPrivateKey, InheritanceFlags.None, PropagationFlags.None);
            sd.DiscretionaryAcl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.WorldSid, null), ReadPublicKey, InheritanceFlags.None, PropagationFlags.None);


            var currentUser = WindowsIdentity.GetCurrent().User;
            if (currentUser != null)
            {
                sd.DiscretionaryAcl.AddAccess(AccessControlType.Allow, currentUser, ReadPublicAndPrivateKey, InheritanceFlags.None, PropagationFlags.None);
            }

            var amsAdmins = registryProvider.AmsAdminSid;
            if (amsAdmins != null)
            {
                sd.DiscretionaryAcl.AddAccess(AccessControlType.Allow, amsAdmins, ReadPublicAndPrivateKey, InheritanceFlags.None, PropagationFlags.None);
            }

            return sd;
        }

        public string GetSecurityDescriptorFromSecret(ProtectedSecret data)
        {
            if (data.Mode != 3)
            {
                throw new ArgumentException("The protected data was of the incorrect format");
            }

            byte[] rawProtectedData = Convert.FromBase64String(data.Data);
            using SafeHGlobalHandle f = new SafeHGlobalHandle(rawProtectedData);

            var result = NCrypt.NCryptUnprotectSecret(out NCrypt.SafeNCRYPT_DESCRIPTOR_HANDLE dh, NCrypt.UnprotectSecretFlags.NCRYPT_SILENT_FLAG | NCrypt.UnprotectSecretFlags.NCRYPT_UNPROTECT_NO_DECRYPT, f.DangerousGetHandle(), f.Size, IntPtr.Zero, IntPtr.Zero, out IntPtr unprotectedData, out uint unprotectedDataSize);

            result.ThrowIfFailed();

            IntPtr ruleStringHandle = IntPtr.Zero;

            try
            {
                result = NCrypt.NCryptGetProtectionDescriptorInfo(
                    dh,
                    IntPtr.Zero,
                    NCrypt.ProtectionDescriptorInfoType.NCRYPT_PROTECTION_INFO_TYPE_DESCRIPTOR_STRING,
                    out ruleStringHandle);

                result.ThrowIfFailed();

                return Marshal.PtrToStringUni(ruleStringHandle);
            }
            finally
            {
                if (ruleStringHandle != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ruleStringHandle);
                }
            }
        }

        public ProtectedSecret ProtectSecret(string secret, CommonSecurityDescriptor securityDescriptor)
        {
            this.licenseManager.ThrowOnMissingFeature(LicensedFeatures.DpapiNgSecretEncryption);

            var result = NCrypt.NCryptCreateProtectionDescriptor($"SDDL={securityDescriptor.GetSddlForm(AccessControlSections.All)}", 0, out NCrypt.SafeNCRYPT_DESCRIPTOR_HANDLE handle);
            result.ThrowIfFailed();

            using (handle)
            {
                using SafeHGlobalHandle f = new SafeHGlobalHandle(Encoding.Unicode.GetBytes(secret));

                result = NCrypt.NCryptProtectSecret(handle, NCrypt.ProtectFlags.NCRYPT_SILENT_FLAG, f.DangerousGetHandle(), f.Size, IntPtr.Zero, IntPtr.Zero, out IntPtr protectedData, out uint protectedDataSize);
                result.ThrowIfFailed();

                using SafeHGlobalHandle d = new SafeHGlobalHandle(protectedData, protectedDataSize, true);

                return new ProtectedSecret
                {
                    Data = Convert.ToBase64String(d.GetBytes(0, (int)protectedDataSize)),
                    Mode = 3
                };
            }
        }

        public ProtectedSecret ProtectSecret(string secret)
        {
            if (dataProtectionOptions.EnableClusterCompatibleSecretEncryption && this.licenseManager.IsFeatureEnabled(LicensedFeatures.DpapiNgSecretEncryption))
            {
                return this.ProtectSecret(secret, this.BuildDefaultSecurityDescriptor());
            }
            else
            {
                return this.ProtectSecretv1(secret);
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

                if (data.Mode == 3)
                {
                    return this.UnprotectSecretv3(data);
                }

                throw new ConfigurationException("The data was protected with an encryption mechanism not known to this version of the application");
            }
            catch (Exception ex)
            {
                throw new ConfigurationException("Unable to decrypt the encrypted data from the configuration file. Use the configuration manager application to re-enter the secret, and try again", ex);
            }
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

        private string UnprotectSecretv3(ProtectedSecret data)
        {
            byte[] rawProtectedData = Convert.FromBase64String(data.Data);
            using SafeHGlobalHandle f = new SafeHGlobalHandle(rawProtectedData);

            var result = NCrypt.NCryptUnprotectSecret(out _, NCrypt.UnprotectSecretFlags.NCRYPT_SILENT_FLAG, f.DangerousGetHandle(), f.Size, IntPtr.Zero, IntPtr.Zero, out IntPtr unprotectedData, out uint unprotectedDataSize);
            result.ThrowIfFailed();

            using SafeHGlobalHandle d = new SafeHGlobalHandle(unprotectedData, unprotectedDataSize, true);
            return Encoding.Unicode.GetString(d.GetBytes(0, (int)unprotectedDataSize));
        }
    }
}
