using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace Lithnet.AccessManager.Api.Providers
{
    public class TokenReplayCache : ITokenReplayCache
    {
        private IMemoryCache cache;
        private readonly object syncObject = new object();

        public TokenReplayCache(IMemoryCache cache)
        {
            this.cache = cache;
        }

        public bool TryAdd(string securityToken, DateTime expiresOn)
        {
            lock (this.syncObject)
            {
                if (this.cache.TryGetValue(securityToken, out _))
                {
                    return false;
                }

                this.cache.Set(securityToken, 0, expiresOn);
                return true;
            }
        }

        public bool TryFind(string securityToken)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            return this.cache.TryGetValue(securityToken, out _);
        }
    }
}
