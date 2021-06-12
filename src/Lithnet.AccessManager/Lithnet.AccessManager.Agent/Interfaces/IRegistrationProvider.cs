using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent
{
    public interface IRegistrationProvider
    {
        Task<RegistrationState> GetRegistrationState();
        bool CanRegisterAgent();
        Task<RegistrationState> RegisterAgent();
    }
}