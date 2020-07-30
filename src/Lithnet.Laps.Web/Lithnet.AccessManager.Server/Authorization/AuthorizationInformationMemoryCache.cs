using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class AuthorizationInformationMemoryCache : MemoryCache, IAuthorizationInformationMemoryCache
    {
        public AuthorizationInformationMemoryCache() : base(new MemoryCacheOptions())
        {
        }
    }
}
