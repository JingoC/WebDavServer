using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Models;

namespace WebDavServer.Infrastructure.FileStorage.Services.Abstract
{
    public interface IVirtualStorageService
    {
        Task<Item?> GetFileInfoAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task<List<Item>> GetDirectoryInfoAsync(PathInfo pathInfo, bool withContent, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task<bool> DirectoryExistsAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task CreateFileAsync(string fileName, PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task CreateDirectoryAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task MoveFileAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default);
        Task MoveDirectoryAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default);
        Task CopyFileAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default);
        Task CopyDirectoryAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
        Task DeleteDirectoryAsync(PathInfo pathInfo, CancellationToken cancellationToken = default);
    }
}
