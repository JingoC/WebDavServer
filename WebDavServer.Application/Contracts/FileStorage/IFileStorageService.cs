using WebDavServer.Application.Contracts.FileStorage.Models.Request;
using WebDavServer.Application.Contracts.FileStorage.Models.Response;

namespace WebDavServer.Application.Contracts.FileStorage
{
    public interface IFileStorageService
    {
        Task<LockResponse> LockAsync(LockRequest request, CancellationToken cancellationToken = default);
        Task UnlockAsync(UnlockRequest request, CancellationToken cancellationToken = default);
        Task<CreateResponse> CreateAsync(CreateRequest request, CancellationToken cancellationToken = default);
        Task<ReadResponse> ReadAsync(ReadRequest request, CancellationToken cancellationToken = default);
        Task<MoveResponse> MoveAsync(MoveRequest r, CancellationToken cancellationToken = default);
        Task<CopyResponse> CopyAsync(CopyRequest r, CancellationToken cancellationToken = default);
        Task<DeleteResponse> DeleteAsync(DeleteRequest request, CancellationToken cancellationToken = default);
        Task<GetPropertiesResponse> GetPropertiesAsync(GetPropertiesRequest request, CancellationToken cancellationToken = default);
    }
}
