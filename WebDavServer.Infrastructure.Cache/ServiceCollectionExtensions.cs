using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WebDavServer.Infrastructure.Cache.Providers;
using WebDavService.Application.Contracts.Cache;

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
