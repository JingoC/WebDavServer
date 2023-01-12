using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebDavServer.Infrastructure.Cache;
using WebDavServer.Infrastructure.FileStorage;
using WebDavServer.Infrastructure.WebDav;

namespace WebDavServer.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
            => services
                .AddCache()
                .AddFileStorage(configuration)
                .AddWebDav()
            ;
    }
}
