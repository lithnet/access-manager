using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface ITokenClaimProvider
    {
        Task AddClaims(SecurityTokenDescriptor token);
    }
}