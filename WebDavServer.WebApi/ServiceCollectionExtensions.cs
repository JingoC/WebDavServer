using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace WebDavServer.WebApi
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApiServices(this IServiceCollection services)
            => services
            .AddControllers().Services
            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebDavServer.WebApi", Version = "v1" });
            })
            ;
    }
}
