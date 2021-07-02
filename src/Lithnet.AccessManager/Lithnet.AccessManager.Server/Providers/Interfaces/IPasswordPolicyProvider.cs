using System.Threading.Tasks;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Server
{
    public interface IPasswordPolicyProvider
    {
        Task<PasswordPolicy> GetPolicy(string deviceId);
    }
}