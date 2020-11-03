using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Lithnet.AccessManager.Service
{
    public class AuthenticationTicketCache : ITicketStore
    {
        private const string KeyPrefix = "AmsAuthenticationSession-";
        private readonly IMemoryCache cache;

        public AuthenticationTicketCache()
        {
            this.cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task<string> StoreAsync(AuthenticationTicket ticket)
        {
            string key = $"{KeyPrefix}{Guid.NewGuid()}";
            await RenewAsync(key, ticket);
            return key;
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket)
        {
            MemoryCacheEntryOptions options = new MemoryCacheEntryOptions();
            DateTimeOffset? expiresUtc = ticket.Properties.ExpiresUtc;

            if (expiresUtc.HasValue)
            {
                options.SetAbsoluteExpiration(expiresUtc.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromHours(1));
            }

            this.cache.Set(key, ticket, options);

            return Task.FromResult(0);
        }

        public Task<AuthenticationTicket> RetrieveAsync(string key)
        {
            this.cache.TryGetValue(key, out AuthenticationTicket ticket);
            return Task.FromResult(ticket);
        }

        public Task RemoveAsync(string key)
        {
            this.cache.Remove(key);
            return Task.FromResult(0);
        }
    }
}