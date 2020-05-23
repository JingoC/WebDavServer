using WebDavServer.Core.Repository;
using WebDavServer.Core.Repository.Implementations;
using WebDavServer.FileStorage.DbContexts;
using WebDavServer.FileStorage.Entities;

namespace WebDavServer.FileStorage.Repository
{
    public interface IItemRepository : IBaseRepository<Item>
    {
    }
    public class ItemRepository : BaseRepository<Item>, IItemRepository
    {
        public ItemRepository(IWebDavDbContext webDavDbContext) : base(webDavDbContext.Items)
        {
        }
    }
}
