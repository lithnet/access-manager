using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager
{
    public interface ICertificateProvider
    {
        X509Store OpenServiceStore(string serviceName, OpenFlags openFlags);

        X509Store OpenServiceStore(OpenFlags flags);

        X509Certificate2 CreateSelfSignedCert(string subject);

        X509Certificate2 FindEncryptionCertificate(string thumbprint = null, string pathHint = null);

        X509Certificate2 FindDecryptionCertificate(string thumbprint);

        X509Certificate2 FindDecryptionCertificate(string thumbprint, string serviceName);

        X509Certificate2Collection GetEligibleCertificates(bool needPrivateKey);

        bool TryGetCertificateFromDirectory(out X509Certificate2 cert, string dnsDomain);

        X509Certificate2 GetCertificateFromDirectory(string dnsDomain);
    }
}