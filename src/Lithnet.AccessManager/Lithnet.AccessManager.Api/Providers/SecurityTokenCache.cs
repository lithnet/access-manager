using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Providers
{
    public class SecurityTokenCache : ISecurityTokenCache
    {
        private static ConcurrentDictionary<string, ClaimsPrincipal> cachedClaims = new ConcurrentDictionary<string, ClaimsPrincipal>();

        public ClaimsPrincipal GetIdentity(string accessToken)
        {
            if (cachedClaims.TryGetValue(accessToken, out ClaimsPrincipal identity))
            {
                return identity;
            }

            throw new UnauthorizedAccessException("The access token was not found");
        }

        public void SetIdentity(string accessToken, ClaimsPrincipal identity)
        {
            if (!cachedClaims.TryAdd(accessToken, identity))
            {
                throw new InvalidOperationException("Could not add the claims identity");
            }
        }
    }
}
