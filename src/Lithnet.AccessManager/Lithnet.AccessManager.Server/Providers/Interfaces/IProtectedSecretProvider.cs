using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server
{
    public interface IProtectedSecretProvider
    {
        string UnprotectSecret(ProtectedSecret data);

        ProtectedSecret ProtectSecret(string secret);

        ProtectedSecret ProtectSecret(string secret, X509Certificate2 cert);

    }
}