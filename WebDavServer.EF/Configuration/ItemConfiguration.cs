using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebDavServer.EF.Entities;

namespace WebDavServer.EF.Configuration
{
    public class ItemConfiguration : IEntityTypeConfiguration<Item>
    {
        public virtual void Configure(EntityTypeBuilder<Item> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();

            builder.Property(a => a.Name).IsRequired();
            builder.Property(a => a.DirectoryId);
            builder.Property(a => a.IsDirectory);
            builder.Property(a => a.Path);
            builder.Property(a => a.Title);
        }
    }
}
