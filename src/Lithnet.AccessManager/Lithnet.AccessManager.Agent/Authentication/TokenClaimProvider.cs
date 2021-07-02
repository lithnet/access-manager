using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.IdentityModel.Tokens;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class TokenClaimProvider : ITokenClaimProvider
    {
        private readonly IAgentSettings settings;
        private readonly IAadJoinInformationProvider aadProvider;

        public TokenClaimProvider(IAgentSettings settings, IAadJoinInformationProvider aadProvider)
        {
            this.settings = settings;
            this.aadProvider = aadProvider;
        }

        public Task AddClaims(SecurityTokenDescriptor token)
        {
            token.Claims ??= new Dictionary<string, object>();

            if (this.settings.AuthenticationMode == AgentAuthenticationMode.Aad)
            {
                token.Claims.Add("auth-mode", this.settings.HasRegisteredSecondaryCredentials ? AgentAuthenticationMode.Ams : AgentAuthenticationMode.Aad);
                if (!this.settings.HasRegisteredSecondaryCredentials)
                {
                    token.Claims.Add("aad-tenant-id", this.aadProvider.TenantId);
                    token.Claims.Add("aad-device-id", this.aadProvider.DeviceId);
                }
            }
            else
            {
                token.Claims.Add("auth-mode", this.settings.AuthenticationMode.ToString());
            }

            return Task.CompletedTask;
        }
    }
}
