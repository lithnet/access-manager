using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.Providers
{
    public interface IRegistrationKeyProvider
    {
        Task<bool> ValidateRegistrationKey(string key);
    }
}