using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Api.Providers
{
    public class SignedAssertionValidator : ISignedAssertionValidator
    {
        private readonly IOptions<SignedAssertionValidationOptions> validatorOptions;

        public SignedAssertionValidator(IOptions<SignedAssertionValidationOptions> validatorOptions)
        {
            this.validatorOptions = validatorOptions;
        }

        public JwtSecurityToken Validate(string assertion, string audience, out X509Certificate2 signingCertificate)
        {
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken rawToken = handler.ReadJwtToken(assertion);

            signingCertificate = this.GetCertificateFromToken(rawToken);

            handler.ValidateToken(assertion, new TokenValidationParameters()
            {
                ValidateAudience = true,
                ValidateLifetime = true, 
                ValidateActor = false,
                ValidateIssuer = false,
                ValidateTokenReplay = true,
                ValidateIssuerSigningKey = false,
                ValidAlgorithms = this.validatorOptions.Value.AllowedSigningAlgorithms,
                ValidAudience = audience,
                IssuerSigningKey = new RsaSecurityKey(signingCertificate.GetRSAPublicKey())
            }, out SecurityToken validatedToken);

            JwtSecurityToken jwt = (JwtSecurityToken)validatedToken;

            if (this.validatorOptions.Value.MaximumAssertionValidityMinutes > 0)
            {
                TimeSpan validityWindow = jwt.ValidTo - jwt.ValidFrom;

                if (this.validatorOptions.Value.MaximumAssertionValidityMinutes < validityWindow.TotalMinutes)
                {
                    throw new SecurityTokenInvalidLifetimeException("The token was generated for a duration longer than the allowable token length");
                }
            }

            return jwt;
        }

        private X509Certificate2 GetCertificateFromToken(JwtSecurityToken securityToken)
        {
            if (securityToken.Header.X5c == null)
            {
                throw new SecurityTokenInvalidSigningKeyException("The x5c claim on the JWT was not present");
            }

            string base64CertificateData = JArray.Parse(securityToken.Header.X5c)[0].ToString();
            return new X509Certificate2(Convert.FromBase64String(base64CertificateData));
        }
    }
}
