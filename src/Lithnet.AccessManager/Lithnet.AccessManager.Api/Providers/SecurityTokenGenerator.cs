using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Lithnet.AccessManager.Api.Shared;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Api.Providers
{
    public class SecurityTokenGenerator : ISecurityTokenGenerator
    {
        private readonly IOptionsMonitor<TokenIssuerOptions> tokenIssuerOptions;
        private readonly IProtectedSecretProvider protectedSecretProvider;
        private readonly IOptionsMonitor<HostingOptions> hostingOptions;

        public SecurityTokenGenerator(IOptionsMonitor<TokenIssuerOptions> tokenIssuerOptions, IProtectedSecretProvider protectedSecretProvider, IOptionsMonitor<HostingOptions> hostingOptions)
        {
            this.tokenIssuerOptions = tokenIssuerOptions;
            this.protectedSecretProvider = protectedSecretProvider;
            this.hostingOptions = hostingOptions;
        }

        public TokenResponse GenerateToken(ClaimsIdentity identity)
        {
            var options = this.tokenIssuerOptions.CurrentValue;

            SymmetricSecurityKey sharedKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.protectedSecretProvider.UnprotectSecret(options.SigningKey)));

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.UtcNow.AddMinutes(options.TokenValidityMinutes),
                Issuer = hostingOptions.CurrentValue.HttpSys.BuildApiHostUrl(),
                Audience = hostingOptions.CurrentValue.HttpSys.BuildApiHostUrl(),
                SigningCredentials = new SigningCredentials(sharedKey, options.SigningAlgorithm)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            string rawToken = tokenHandler.WriteToken(token);

            return new TokenResponse(rawToken, tokenDescriptor.Expires.Value);
        }
    }
}
