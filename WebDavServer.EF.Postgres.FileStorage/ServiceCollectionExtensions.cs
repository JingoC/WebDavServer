using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebDavServer.EF.Postgres.FileStorage
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileStorageDbContext(this IServiceCollection services,
            IConfiguration configuration)
            => services
                .AddSingleton(configuration.GetSection("FileStoragePostgresConfiguration")
                    .Get<FileStoragePostgresConfiguration>()!)
                .EnableLegacy()
                .AddDbContext<FileStorageDbContext, FileStoragePostgresDbContext>();

        public static IServiceCollection EnableLegacy(this IServiceCollection services)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            return services;
        }

        public static void ApplyMigrations(this IServiceProvider rootServiceProvider)
        {
            using var scope = rootServiceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<FileStorageDbContext>().Database.Migrate();
        }
    }
}