using WebDavServer.Application.Contracts.WebDav.Models.Request;
using WebDavServer.Application.Contracts.WebDav.Models.Response;

namespace WebDavServer.Application.Contracts.WebDav
{
    public interface IWebDavService
    {
        Task<string> PropfindAsync(PropfindRequest r, CancellationToken cancellationToken = default);
        Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default);
        Task MkColAsync(string path, CancellationToken cancellationToken = default);
        Task DeleteAsync(string path, CancellationToken cancellationToken = default);
        Task MoveAsync(string srcPath, string dstPath, CancellationToken cancellationToken = default);
        Task CopyAsync(string srcPath, string dstPath, CancellationToken cancellationToken = default);
        Task PutAsync(string path, Stream stream, CancellationToken cancellationToken = default);
        Task<LockResponse> LockAsync(LockRequest r, CancellationToken cancellationToken = default);
        Task UnlockAsync(string path, CancellationToken cancellationToken = default);
    }
}
