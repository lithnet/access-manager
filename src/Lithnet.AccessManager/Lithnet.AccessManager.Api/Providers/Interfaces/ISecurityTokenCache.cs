using System.Security.Claims;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface ISecurityTokenCache
    {
        ClaimsPrincipal GetIdentity(string accessToken);

        void SetIdentity(string accessToken, ClaimsPrincipal identity);
    }
}