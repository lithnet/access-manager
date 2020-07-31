using Microsoft.Extensions.Caching.Memory;

namespace Lithnet.AccessManager.Server.Authorization
{
    public interface ITargetDataCache : IMemoryCache
    {
    }
}