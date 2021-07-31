using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface ITokenClaimProvider
    {
        IEnumerable<Claim> GetClaims();
    }
}