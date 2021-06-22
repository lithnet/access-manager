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
        private readonly ISettingsProvider settings;
        private readonly IAadJoinInformationProvider aadProvider;

        public TokenClaimProvider(ISettingsProvider settings, IAadJoinInformationProvider aadProvider)
        {
            this.settings = settings;
            this.aadProvider = aadProvider;
        }

        public async Task AddClaims(SecurityTokenDescriptor token)
        {
            token.Claims ??= new Dictionary<string, object>();
            token.Claims.Add("auth-mode", this.settings.AuthenticationMode.ToString());

            if (this.settings.AuthenticationMode == AgentAuthenticationMode.Aadj)
            {
                token.Claims.Add("aad-tenant-id", await this.aadProvider.GetTenantId());
                token.Claims.Add("aad-device-id", await this.aadProvider.GetDeviceId());
            }
        }
    }
}
