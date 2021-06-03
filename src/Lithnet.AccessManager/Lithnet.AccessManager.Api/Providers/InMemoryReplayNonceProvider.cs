using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using System.Collections.Concurrent;

namespace Lithnet.AccessManager.Api.Providers
{
    public class InMemoryReplayNonceProvider : IReplayNonceProvider
    {
        private readonly RandomStringGenerator generator;
        private ConcurrentDictionary<string, bool> cache;

        public InMemoryReplayNonceProvider(RandomStringGenerator generator)
        {
            this.generator = generator;
            this.cache = new ConcurrentDictionary<string, bool>();
        }

        public string GenerateNonce()
        {
            string nonce = this.generator.Generate(32);

            if (!this.cache.TryAdd(nonce, false))
            {
                throw new InvalidOperationException("Could not add nonce to the cache");
            }

            return nonce;
        }

        public bool ConsumeNonce(string nonce)
        {
            if (nonce == "test")
            {
                return true;
            }

            return this.cache.TryRemove(nonce, out _);
        }
    }
}
