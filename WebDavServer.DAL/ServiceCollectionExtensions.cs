using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebDavServer.FileStorage.DbContexts;
using WebDavServer.FileStorage.Options;
using WebDavServer.FileStorage.Repository;
using WebDavServer.FileStorage.Services;

namespace WebDavServer.FileStorage
{
    public static class ServiceCollectionExtensions
    {
        static public void AddFileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileStorageOptions>(configuration.GetSection(nameof(FileStorageOptions)));

            services.AddDbContext<WebDavDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("WebDavFileStorageDb"))
            );

            services.AddScoped<IWebDavDbContext, WebDavDbContext>();

            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IDataItemRepository, DataItemRepository>();

            services.AddScoped<IFileStorageService, FileStorageService>();
        }
    }
}
