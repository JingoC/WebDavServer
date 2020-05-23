using Microsoft.EntityFrameworkCore;
using WebDavServer.FileStorage.Entities;

namespace WebDavServer.FileStorage.DbContexts
{
    public interface IWebDavDbContext
    {
        DbSet<Item> Items { get; set; }
        DbSet<DataItem> DataItems { get; set; }
    }

    public class WebDavDbContext : DbContext, IWebDavDbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<DataItem> DataItems { get; set; }
        public WebDavDbContext(DbContextOptions<WebDavDbContext> options) : base(options)
        {

        }
    }
}
