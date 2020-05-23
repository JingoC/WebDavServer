using System.Threading.Tasks;

namespace WebDavServer.Core.Repository
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {
        Task<TEntity> InsertAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
    }
}
