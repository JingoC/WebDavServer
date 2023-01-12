using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebDavServer.Infrastructure.FileStorage.Options;
using WebDavServer.Infrastructure.FileStorage.Services;
using WebDavService.Application.Contracts.FileStorage;

namespace WebDavServer.Infrastructure.FileStorage
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
            => services
                .Configure<FileStorageOptions>(configuration.GetSection(nameof(FileStorageOptions)))
                .AddScoped<IFileStorageService, FileStorageService>();
    }
}