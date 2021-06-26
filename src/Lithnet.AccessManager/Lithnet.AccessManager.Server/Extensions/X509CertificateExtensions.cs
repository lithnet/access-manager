using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public static class X509CertificateExtensions
    {
        public static FileSecurity GetPrivateKeySecurity(this X509Certificate2 cert)
        {
            string location = GetPrivateKeyPath(cert);
            
            if (location == null)
            {
                throw new CertificateNotFoundException("The certificate private key was not found");
            }

            FileInfo info = new FileInfo(location);
            return info.GetAccessControl();
        }

        public static void SetPrivateKeySecurity(this X509Certificate2 cert, FileSecurity security)
        {
            string location = GetPrivateKeyPath(cert);

            if (location == null)
            {
                throw new CertificateNotFoundException("The certificate private key was not found");
            }

            FileInfo info = new FileInfo(location);
            info.SetAccessControl(security);
        }

        public static void AddPrivateKeyReadPermission(this X509Certificate2 cert, IdentityReference account)
        {
            string location = GetPrivateKeyPath(cert);

            if (location == null)
            {
                throw new CertificateNotFoundException("The certificate private key was not found. Manually add permissions for the service account to read this private key");
            }

            FileInfo info = new FileInfo(location);
            info.AddFileSecurity(account, FileSystemRights.Read, AccessControlType.Allow);
        }

        public static string GetPrivateKeyPath(X509Certificate2 cert)
        {
            RSACng cng = cert.PrivateKey as RSACng;
            RSACryptoServiceProvider crypto = cert.PrivateKey as RSACryptoServiceProvider;

            string name = cng?.Key?.UniqueName ?? crypto?.CspKeyContainerInfo?.UniqueKeyContainerName;

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Application Data\Microsoft\Crypto\RSA\MachineKeys", name);

            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Application Data\Microsoft\Crypto\Keys", name);

            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }
    }
}
