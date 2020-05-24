using WebDavServer.Core.Repository;
using WebDavServer.Core.Repository.Implementations;
using WebDavServer.FileStorage.DbContexts;
using WebDavServer.FileStorage.Entities;

namespace WebDavServer.FileStorage.Repository
{
    public interface IDataItemRepository : IBaseRepository<DeleteItem>
    {
    }
    public class DeleteItemRepository : BaseRepository<DeleteItem>, IDataItemRepository
    {
        public DeleteItemRepository(IWebDavDbContext webDavDbContext) : base(webDavDbContext.DeleteItems)
        {
        }
    }
}
