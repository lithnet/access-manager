using Microsoft.IdentityModel.Tokens;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface ITokenClaimProvider
    {
        void AddClaims(SecurityTokenDescriptor token);
    }
}