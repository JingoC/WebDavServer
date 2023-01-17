using WebDavServer.Infrastructure.FileStorage.Models;

namespace WebDavServer.Infrastructure.FileStorage.Services.Abstract
{
    public interface IPathService
    {
        Task<PathInfo> GetDestinationPathInfoAsync(string relativePath, CancellationToken cancellationToken = default);
    }
}
