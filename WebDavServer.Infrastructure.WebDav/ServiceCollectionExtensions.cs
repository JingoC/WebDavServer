using Microsoft.Extensions.DependencyInjection;
using WebDavService.Application.Contracts.WebDav;

namespace WebDavServer.Infrastructure.WebDav
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebDav(this IServiceCollection services)
            => services
                .AddScoped<IWebDavService, Services.WebDavService>();
    }
}
