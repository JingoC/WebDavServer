using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebDavServer.Application.Contracts.FileStorage;
using WebDavServer.Infrastructure.FileStorage.Options;
using WebDavServer.Infrastructure.FileStorage.Services;

namespace WebDavServer.Infrastructure.FileStorage
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
            => services
                .Configure<FileStorageOptions>(configuration.GetSection(nameof(FileStorageOptions)))
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

            var diskC = Path.Combine(options.Path, "C");
            var diskD = Path.Combine(options.Path, "D");

            if (!Directory.Exists(diskC))
                Directory.CreateDirectory(diskC);

            if (!Directory.Exists(diskD))
                Directory.CreateDirectory(diskD);
        }
    }
}