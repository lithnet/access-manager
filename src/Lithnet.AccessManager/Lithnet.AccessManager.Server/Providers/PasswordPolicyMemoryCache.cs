using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class PasswordPolicyMemoryCache : MemoryCache, IPasswordPolicyMemoryCache
    {
        public PasswordPolicyMemoryCache() : base(new MemoryCacheOptions())
        {
        }
    }
}
