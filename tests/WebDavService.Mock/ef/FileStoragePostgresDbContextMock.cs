using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory.ValueGeneration.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using WebDavServer.EF;
using WebDavServer.EF.Configuration;
using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Services;

namespace WebDavService.Mock.ef
{
    public class FileStoragePostgresDbContextMock : FileStorageDbContext
    {
        public FileStoragePostgresDbContextMock(DbContextOptions<FileStoragePostgresDbContextMock> options)
        {

        }

        public FileStoragePostgresDbContextMock AddFile(long id, string? title = null, long? directoryId = null, string? name = null)
        {
            title = title ?? Guid.NewGuid().ToString();
            name = name ?? Guid.NewGuid().ToString();

            Set<Item>().Add(new Item()
            {
                Id = id,
                DirectoryId = directoryId,
                IsDirectory = false,
                Name = name,
                Title = title
            });
            SaveChanges();

            return this;
        }

        public FileStoragePostgresDbContextMock AddDirectory(long id, string? title = null, long? directoryId = null)
        {
            title = title ?? Guid.NewGuid().ToString();

            Set<Item>().Add(new Item()
            {
                Id = id,
                DirectoryId = directoryId,
                IsDirectory = true,
                Name = Guid.NewGuid().ToString(),
                Title = title
            });
            SaveChanges();

            return this;
        }

        public static FileStoragePostgresDbContextMock Create()
        {
            var builder = new DbContextOptionsBuilder<FileStoragePostgresDbContextMock>();
            builder.UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));

            var options = builder.Options;
            var personDataContext = new FileStoragePostgresDbContextMock(options);
            personDataContext.Database.EnsureDeleted();
            personDataContext.Database.EnsureCreated();

            personDataContext.Set<Item>().Add(new Item
            {
                CreatedDate = DateTime.Now,
                IsDirectory = true,
                Title = PathService.RootDirectory,
                Id = 100,
                Name = PathService.RootDirectory
            });

            personDataContext.SaveChanges();

            return personDataContext;
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            Assembly assembly = typeof(ItemConfiguration).Assembly;
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

            var autoGenLongProperties = modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(long) && p.ValueGenerated == ValueGenerated.OnAdd);

            foreach (var property in autoGenLongProperties)
                property.SetValueGeneratorFactory((p, t) => new InMemoryIntegerValueGenerator<long>(p.GetIndex()));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
        }
    }
}
