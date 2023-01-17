using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Models;

namespace WebDavServer.Infrastructure.FileStorage.Services.Abstract
{
    public interface IVirtualStorageService
    {
        Task<Item?> GetFileInfoAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task<List<Item>> GetDirectoryInfoAsync(PathInfo pathInfo, bool withContent, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task CreateFileAsync(string fileName, PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task CreateDirectoryAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);

    }
}
