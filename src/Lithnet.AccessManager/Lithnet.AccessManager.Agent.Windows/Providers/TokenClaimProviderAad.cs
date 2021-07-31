using Lithnet.AccessManager.Api.Shared;
using System.Collections.Generic;
using System.Security.Claims;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class TokenClaimProviderAad : ITokenClaimProvider
    {
        private readonly IAgentSettings settings;
        private readonly IAadJoinInformationProvider aadProvider;

        public TokenClaimProviderAad(IAgentSettings settings, IAadJoinInformationProvider aadProvider)
        {
            this.settings = settings;
            this.aadProvider = aadProvider;
        }

        public IEnumerable<Claim> GetClaims()
        {
            if (this.settings.AuthenticationMode == AgentAuthenticationMode.Aad)
            {
                yield return new Claim(AmsClaimNames.AuthMode, (this.settings.HasRegisteredSecondaryCredentials ? AgentAuthenticationMode.Ams : AgentAuthenticationMode.Aad).ToString());

                if (!this.settings.HasRegisteredSecondaryCredentials)
                {
                    yield return new Claim(AmsClaimNames.AadTenantId, this.aadProvider.TenantId);
                    yield return new Claim(AmsClaimNames.AadDeviceId, this.aadProvider.DeviceId);
                }
            }
        }
    }
}
