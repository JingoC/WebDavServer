using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WebDavServer.Core.Providers;

namespace WebDavServer.Core
{
    public static class ServiceCollectionExtensions
    {
        static public void AddCache(this IServiceCollection services)
        {
            services.AddSingleton<IMemoryCache, MemoryCache>();
            services.AddSingleton<ICacheProvider, CacheProvider>();
        }
    }
}
