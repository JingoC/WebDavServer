using Microsoft.Extensions.Caching.Memory;
using WebDavService.Application.Contracts.Cache;

namespace WebDavServer.Infrastructure.Cache.Providers
{
    public class CacheProvider : ICacheProvider
    {
        private class EmptyResultClass
        {
        }

        private readonly IMemoryCache _memoryCache;


        public CacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void Remove(string cacheKey)
        {
            _memoryCache.Remove(cacheKey);
        }

        public T Get<T>(string cacheKey, int cacheTimeInMinutes, Func<T> func)
        {
            var cachedObject = _memoryCache.Get(cacheKey);

            if (cachedObject != null)
            {
                if (cachedObject is EmptyResultClass)
                    return default;

                return (T)cachedObject;
            }


            var expensiveObject = func();

            var absoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(cacheTimeInMinutes));

            if (expensiveObject == null)
            {
                _memoryCache.Set(key: cacheKey,
                    value: new EmptyResultClass(),
                    absoluteExpiration: absoluteExpiration);

                return default;
            }

            _memoryCache.Set(cacheKey, expensiveObject, absoluteExpiration);

            return expensiveObject;
        }

        public void Set<T>(string cacheKey, T cacheValue)
        {
            _memoryCache.Set<T>(cacheKey, cacheValue);
        }

        public async Task<T> GetAsync<T>(string cacheKey, int cacheTimeInMinutes, Func<Task<T>> func)
        {
            var cachedObject = _memoryCache.Get(cacheKey);

            if (cachedObject != null && !(cachedObject is EmptyResultClass))
            {
                return (T)cachedObject;
            }

            var expensiveObject = await func().ConfigureAwait(false);

            var absoluteExpiration = new DateTimeOffset(DateTime.Now.AddMinutes(cacheTimeInMinutes));

            if (expensiveObject == null)
            {
                _memoryCache.Set(key: cacheKey,
                    value: new EmptyResultClass(),
                    absoluteExpiration: absoluteExpiration);

                return default;
            }

            _memoryCache.Set(cacheKey, expensiveObject, absoluteExpiration);

            return expensiveObject;
        }
    }
}
