using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager
{
    public interface ICertificateProvider
    {
        X509Certificate2 CreateSelfSignedCert(string subject);

        X509Certificate2 FindCertificate(bool requirePrivateKey, string thumbprint, string pathHint);

        X509Certificate2 GetCertificateWithPrivateKey(string thumbprint);

        X509Certificate2Collection GetEligibleCertificates(bool needPrivateKey);

        bool TryGetCertificateFromDirectory(out X509Certificate2 cert, string dnsDomain);

        X509Certificate2 GetCertificateFromDirectory(string dnsDomain);

        void PublishCertificateToDirectory(X509Certificate2 cert, string dnsDomain);
    }
}