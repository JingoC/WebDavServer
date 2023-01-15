using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WebDavServer.Application.Contracts.Cache;
using WebDavServer.Infrastructure.Cache.Providers;

namespace WebDavServer.Infrastructure.Cache
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCache(this IServiceCollection services)
            => services
                .AddSingleton<IMemoryCache, MemoryCache>()
                .AddSingleton<ICacheProvider, CacheProvider>()
            ;
    }
}
