using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.UI
{
    public class UIiOptionsMonitorCache<TOptions> : IOptionsMonitorCache<TOptions> where TOptions : class
    {
        private MemoryCache cache = new MemoryCache(new MemoryCacheOptions
        {
            Clock = new SystemClock(),
            CompactionPercentage = 0
        });

        public TOptions GetOrAdd(string name, Func<TOptions> createOptions)
        {
            return cache.GetOrCreate<TOptions>(name, (x) => createOptions.Invoke());
        }

        public bool TryAdd(string name, TOptions options)
        {
            cache.CreateEntry(name).Value = options;
            return true;
        }

        public bool TryRemove(string name)
        {
            cache.Remove(name);
            return true;
        }

        public void Clear()
        {
            cache = new MemoryCache(new MemoryCacheOptions
            {
                Clock = new SystemClock(),
                CompactionPercentage = 0
            });
        }
    }
}

