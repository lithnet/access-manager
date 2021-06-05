using System.Collections.Generic;
using System.Security.Claims;
using Lithnet.AccessManager.Api.Models;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface ISecurityTokenGenerator
    {
        TokenResponse GenerateToken(ClaimsIdentity identity);
    }
}