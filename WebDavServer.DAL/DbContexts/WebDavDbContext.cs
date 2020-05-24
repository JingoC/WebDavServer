using Microsoft.EntityFrameworkCore;
using WebDavServer.FileStorage.Entities;

namespace WebDavServer.FileStorage.DbContexts
{
    public interface IWebDavDbContext
    {
        DbSet<DeleteItem> DeleteItems { get; set; }
    }

    public class WebDavDbContext : DbContext, IWebDavDbContext
    {
        public DbSet<DeleteItem> DeleteItems { get; set; }
        public WebDavDbContext(DbContextOptions<WebDavDbContext> options) : base(options)
        {

        }
    }
}
