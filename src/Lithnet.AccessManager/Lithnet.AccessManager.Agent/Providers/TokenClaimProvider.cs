using Lithnet.AccessManager.Api.Shared;
using System.Collections.Generic;
using System.Security.Claims;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class TokenClaimProvider : ITokenClaimProvider
    {
        private readonly IAgentSettings settings;

        public TokenClaimProvider(IAgentSettings settings)
        {
            this.settings = settings;
        }

        public IEnumerable<Claim> GetClaims()
        {
            if (this.settings.AuthenticationMode != AgentAuthenticationMode.Aad)
            {
                yield return new Claim(AmsClaimNames.AuthMode, this.settings.AuthenticationMode.ToString());
            }
        }
    }
}
