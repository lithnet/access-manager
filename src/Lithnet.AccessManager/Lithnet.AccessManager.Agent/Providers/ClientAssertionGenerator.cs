using Lithnet.AccessManager.Agent.Providers;
using Lithnet.AccessManager.Api.Shared;
using Lithnet.AccessManager.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class ClientAssertionProvider : IClientAssertionProvider
    {
        private readonly IRandomValueGenerator rvg;

        public ClientAssertionProvider(IRandomValueGenerator rvg)
        {
            this.rvg = rvg;
        }

        public async Task<ClientAssertion> BuildAssertion(X509Certificate2 cert, string audience)
        {
            return await this.BuildAssertion(cert, audience, new List<Claim>());
        }

        public Task<ClientAssertion> BuildAssertion(X509Certificate2 cert, string audience, IEnumerable<Claim> additionalClaims)
        {
            string exportedCertificate = Convert.ToBase64String(cert.Export(X509ContentType.Cert));

            string myIssuer = Environment.MachineName;

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("jti", this.rvg.GenerateRandomString(32))
                }),
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(4),
                Issuer = myIssuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new X509SecurityKey(cert), SecurityAlgorithms.RsaSha256)
            };

            if (additionalClaims != null)
            {
                tokenDescriptor.Subject.AddClaims(additionalClaims);
            }

            // Add x5c header parameter containing the signing certificate:
            JwtSecurityToken jwt = (JwtSecurityToken)tokenHandler.CreateToken(tokenDescriptor);
            jwt.Header.Add(JwtHeaderParameterNames.X5c, new List<string> { exportedCertificate });

            return Task.FromResult(new ClientAssertion
            {
                Assertion = tokenHandler.WriteToken(jwt)
            });
        }
    }
}
