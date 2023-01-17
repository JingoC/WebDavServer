using Microsoft.EntityFrameworkCore;
using WebDavServer.EF.Postgres.FileStorage.Configuration;

namespace WebDavServer.EF.Postgres.FileStorage
{
    public class FileStoragePostgresDbContext : FileStorageDbContext
    {
        private readonly FileStoragePostgresConfiguration _configuration;

        public FileStoragePostgresDbContext(FileStoragePostgresConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseNpgsql(_configuration.GetFullConnectionString());
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(_configuration.Schema);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ItemPostgresConfiguration).Assembly);
        }
    }
}
