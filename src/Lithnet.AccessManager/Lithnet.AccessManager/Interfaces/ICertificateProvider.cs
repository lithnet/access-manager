using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager
{
    public interface ICertificateProvider
    {
        X509Certificate2 FindEncryptionCertificate();

        X509Certificate2 FindEncryptionCertificate(string thumbprint);

        X509Certificate2 FindDecryptionCertificate(string thumbprint);

        X509Certificate2Collection GetEligiblePasswordEncryptionCertificates(bool needPrivateKey);

        X509Certificate2Collection GetEligibleClusterEncryptionCertificates(bool needPrivateKey);

        bool TryGetCertificateFromDirectory(out X509Certificate2 cert, string dnsDomain);

        X509Certificate2 CreateSelfSignedCert(string subject, Oid eku);
    }
}