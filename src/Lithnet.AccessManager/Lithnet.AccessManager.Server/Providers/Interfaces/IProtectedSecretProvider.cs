using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server
{
    public interface IProtectedSecretProvider
    {
        string UnprotectSecret(ProtectedSecret data);

        ProtectedSecret ProtectSecret(string secret);
    }
}