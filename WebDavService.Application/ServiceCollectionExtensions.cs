using Microsoft.Extensions.DependencyInjection;

namespace WebDavService.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
            => services;
    }
}
