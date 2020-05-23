using Microsoft.Extensions.DependencyInjection;
using WebDavServer.WebDav.Services;

namespace WebDavServer.WebDav
{
    public static class ServiceCollectionExtensions
    {
        static public void AddWebDav(this IServiceCollection services)
        {
            services.AddScoped<IWebDavService, WebDavService>();
        }
    }
}
