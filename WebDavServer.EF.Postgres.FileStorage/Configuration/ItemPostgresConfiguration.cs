using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebDavServer.EF.Configuration;
using WebDavServer.EF.Entities;

namespace WebDavServer.EF.Postgres.FileStorage.Configuration
{
    public class ItemPostgresConfiguration : ItemConfiguration
    {
        public override void Configure(EntityTypeBuilder<Item> builder)
        {
            base.Configure(builder);
            builder.ToTable("Items");

            builder.Property(a => a.Id).HasColumnName("Id");
            builder.Property(a => a.Name).HasColumnName("Name");
            builder.Property(a => a.IsDirectory).HasColumnName("IsDirectory");
            builder.Property(a => a.DirectoryId).HasColumnName("DirectoryId");
            builder.Property(a => a.Title).HasColumnName("Title");
            builder.Property(a => a.Path).HasColumnName("Path");
        }
    }
}
