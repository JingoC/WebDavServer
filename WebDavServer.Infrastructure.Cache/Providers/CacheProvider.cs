using Microsoft.Extensions.Caching.Memory;
using WebDavServer.Application.Contracts.Cache;

namespace WebDavServer.Infrastructure.Cache.Providers
{
    public class CacheProvider : ICacheProvider
    {
        private readonly IMemoryCache _memoryCache;
        
        public CacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _memoryCache.Remove(key);

            return Task.CompletedTask;
        }
        
        public async Task<T?> GetOrSetAsync<T>(string key, int lifeTimePerMinute, 
            Func<CancellationToken, Task<T>> setFunctionAsync, CancellationToken cancellationToken = default)
        {
            // TODO: Use semaphoreSlim

            var cachedObject = _memoryCache.Get(key);

            if (cachedObject is not null)
            {
                return (T)cachedObject;
            }

            var expensiveObject = await setFunctionAsync(cancellationToken);

            await SetAsync(key, expensiveObject, lifeTimePerMinute, cancellationToken);

            return expensiveObject;
        }

        private Task SetAsync<T>(string key, T cacheValue, int lifeTimePerMinute,
            CancellationToken cancellationToken = default)
        {
            var absoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(lifeTimePerMinute));

            _memoryCache.Set(key, cacheValue, absoluteExpiration);

            return Task.CompletedTask;
        }
    }
}
