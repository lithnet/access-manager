using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager
{
    public interface ICertificateResolver
    {
        X509Certificate2 GetDecryptionCertificate(string thumbprint);

        X509Certificate2 GetEncryptionCertificate(string thumbprint);
    }
}