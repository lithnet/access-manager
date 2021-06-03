using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;

namespace Lithnet.AccessManager.Api.Providers
{
    public class SelfSignedAssertionValidator : ISelfSignedAssertionValidator
    {
        private readonly IReplayNonceProvider nonceProvider;

        public SelfSignedAssertionValidator(IReplayNonceProvider nonceProvider)
        {
            this.nonceProvider = nonceProvider;
        }

        public JwtSecurityToken Validate(string assertion, string audience, out X509Certificate2 signingCertificate)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken rawToken = handler.ReadJwtToken(assertion);

            signingCertificate = this.GetCertificateFromToken(rawToken);

            handler.ValidateToken(assertion, new TokenValidationParameters()
            {
                ValidateAudience = true,
                ValidateIssuer = false,
                ValidateLifetime = true,
                ValidAlgorithms = new[]
                {
                    SecurityAlgorithms.RsaSha256,
                    SecurityAlgorithms.RsaSha384,
                    SecurityAlgorithms.RsaSha512,
                    SecurityAlgorithms.RsaSsaPssSha256,
                    SecurityAlgorithms.RsaSsaPssSha384,
                    SecurityAlgorithms.RsaSsaPssSha512
                },
                ValidAudience = audience,
                IssuerSigningKey = new RsaSecurityKey(signingCertificate.GetRSAPublicKey())
            }, out SecurityToken validatedToken);

            JwtSecurityToken jwt = (JwtSecurityToken)validatedToken;

            string nonce = jwt.Claims.FirstOrDefault(t => t.Type == "nonce")?.Value;

            if (nonce == null)
            {
                throw new AssertionMissingNonceException();
            }

            if (!this.nonceProvider.ConsumeNonce(nonce))
            {
                throw new AssertionReplayException();
            }

            return jwt;
        }

        private X509Certificate2 GetCertificateFromToken(JwtSecurityToken securityToken)
        {
            string base64CertificateData = JArray.Parse(securityToken.Header.X5c)[0].ToString();
            return new X509Certificate2(Convert.FromBase64String(base64CertificateData));
        }
    }
}
