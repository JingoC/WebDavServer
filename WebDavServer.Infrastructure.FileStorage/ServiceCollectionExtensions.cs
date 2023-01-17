using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebDavServer.Application.Contracts.FileStorage;
using WebDavServer.EF;
using WebDavServer.EF.Entities;
using WebDavServer.EF.Postgres.FileStorage;
using WebDavServer.Infrastructure.FileStorage.Options;
using WebDavServer.Infrastructure.FileStorage.Services;
using WebDavServer.Infrastructure.FileStorage.Services.Abstract;

namespace WebDavServer.Infrastructure.FileStorage
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
            => services
                .Configure<FileStorageOptions>(configuration.GetSection(nameof(FileStorageOptions)))
                .AddFileStorageDbContext(configuration)
                .AddScoped<IPathService, PathService>()
                .AddScoped<IPhysicalStorageService, PhysicalStorageService>()
                .AddScoped<IVirtualStorageService, VirtualStorageService>()
                .AddScoped<IFileStorageService, FileStorageService>();

        public static void FileStorageInitialize(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<FileStorageService>>();

            if (string.IsNullOrWhiteSpace(options.Path))
                logger.LogError($"{nameof(FileStorageService)}.{nameof(FileStorageOptions.Path)} not set");
            if (string.IsNullOrWhiteSpace(options.RecyclerPath))
                logger.LogError($"{nameof(FileStorageService)}.{nameof(FileStorageOptions.RecyclerPath)} not set");
            if (string.IsNullOrWhiteSpace(options.RecyclerName))
                logger.LogError($"{nameof(FileStorageService)}.{nameof(FileStorageOptions.RecyclerName)} not set");

            var dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();

            if (!dbContext.Set<Item>().Any(x => x.IsDirectory && x.Title == "C"))
            {
                // TODO: fix stub

                var rootDirectory = new Item
                {
                    IsDirectory = true,
                    Name = "/",
                    Title = "/"
                };

                dbContext.Set<Item>().Add(rootDirectory);

                dbContext.SaveChanges();

                rootDirectory = dbContext.Set<Item>().First(x => x.Title == "/");

                dbContext.Set<Item>().Add(new Item
                {
                    IsDirectory = true,
                    Name = "C",
                    Title = "C",
                    DirectoryId = rootDirectory.Id
                });

                dbContext.Set<Item>().Add(new Item
                {
                    IsDirectory = true,
                    Name = "D",
                    Title = "D",
                    DirectoryId = rootDirectory.Id
                });

                dbContext.SaveChanges();
            }
        }
    }
}