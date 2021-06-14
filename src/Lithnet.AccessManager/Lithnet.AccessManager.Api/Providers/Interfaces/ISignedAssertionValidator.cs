using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface ISignedAssertionValidator
    {
        JwtSecurityToken Validate(string assertion, string audiencePath, out X509Certificate2 signingCertificate);
    }
}