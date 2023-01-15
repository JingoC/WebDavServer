using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.WebDav.Models.Request;
using WebDavServer.Application.Contracts.WebDav.Models.Response;

namespace WebDavServer.Application.Contracts.WebDav
{
    public interface IWebDavService
    {
        Task<string> PropfindAsync(PropfindRequest request, CancellationToken cancellationToken = default);
        Task<Stream> GetAsync(string path, CancellationToken cancellationToken = default);
        Task<ErrorType> MkColAsync(string path, CancellationToken cancellationToken = default);
        Task<ErrorType> DeleteAsync(string path, CancellationToken cancellationToken = default);
        Task<ErrorType> MoveAsync(MoveRequest request, CancellationToken cancellationToken = default);
        Task<ErrorType> CopyAsync(CopyRequest request, CancellationToken cancellationToken = default);
        Task PutAsync(string path, Stream stream, CancellationToken cancellationToken = default);
        Task<LockResponse> LockAsync(LockRequest request, CancellationToken cancellationToken = default);
        Task UnlockAsync(string path, CancellationToken cancellationToken = default);
    }
}
