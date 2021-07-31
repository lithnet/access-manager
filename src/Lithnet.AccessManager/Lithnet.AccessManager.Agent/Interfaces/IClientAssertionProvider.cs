using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IClientAssertionProvider
    {
        Task<ClientAssertion> BuildAssertion(X509Certificate2 cert, string audience, IEnumerable<Claim> additionalClaims);

        Task<ClientAssertion> BuildAssertion(X509Certificate2 cert, string audience);
    }
}