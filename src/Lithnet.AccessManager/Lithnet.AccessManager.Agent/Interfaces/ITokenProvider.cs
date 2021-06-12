using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public interface ITokenProvider
    {
        Task<string> GetAccessToken();
    }
}