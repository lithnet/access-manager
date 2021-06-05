using Lithnet.AccessManager.Api.Models;
using Lithnet.AccessManager.Server;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Lithnet.AccessManager.Api.Providers
{
    public class SecurityTokenGenerator : ISecurityTokenGenerator
    {
        private readonly IOptionsMonitor<TokenIssuerOptions> tokenIssuerOptions;
        private readonly IProtectedSecretProvider protectedSecretProvider;

        public SecurityTokenGenerator(IOptionsMonitor<TokenIssuerOptions> tokenIssuerOptions, IProtectedSecretProvider protectedSecretProvider)
        {
            this.tokenIssuerOptions = tokenIssuerOptions;
            this.protectedSecretProvider = protectedSecretProvider;
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
                Issuer = options.Issuer,
                Audience = options.Audience,
                SigningCredentials = new SigningCredentials(sharedKey, options.SigningAlgorithm)
            };

            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            string rawToken = tokenHandler.WriteToken(token);

            return new TokenResponse(rawToken, tokenDescriptor.Expires.Value);
        }
    }
}
