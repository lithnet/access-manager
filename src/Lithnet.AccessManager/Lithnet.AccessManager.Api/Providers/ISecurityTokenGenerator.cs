using System.Collections.Generic;
using System.Security.Claims;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface ISecurityTokenGenerator
    {
       // string GenerateToken(ClaimsPrincipal principal);
        //string GenerateToken(string subject, IEnumerable<Claim> claims);

        string GenerateToken(IList<Claim> claims);
        string GenerateToken(ClaimsIdentity identity);
    }
}