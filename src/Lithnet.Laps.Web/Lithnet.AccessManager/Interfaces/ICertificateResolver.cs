using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager
{
    public interface ICertificateResolver
    {
        X509Certificate2 FindCertificate(bool requirePrivateKey, string thumbprint, string pathHint);

        X509Certificate2 GetCertificateWithPrivateKey(string thumbprint);
    }
}