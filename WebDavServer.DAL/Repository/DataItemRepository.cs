using WebDavServer.Core.Repository;
using WebDavServer.Core.Repository.Implementations;
using WebDavServer.FileStorage.DbContexts;
using WebDavServer.FileStorage.Entities;

namespace WebDavServer.FileStorage.Repository
{
    public interface IDataItemRepository : IBaseRepository<DataItem>
    {
    }
    public class DataItemRepository : BaseRepository<DataItem>, IDataItemRepository
    {
        public DataItemRepository(IWebDavDbContext webDavDbContext) : base(webDavDbContext.DataItems)
        {
        }
    }
}
