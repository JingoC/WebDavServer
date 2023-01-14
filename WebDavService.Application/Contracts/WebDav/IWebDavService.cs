using WebDavService.Application.Contracts.FileStorage.Models;
using WebDavService.Application.Contracts.WebDav.Models;

namespace WebDavService.Application.Contracts.WebDav
{
    public interface IWebDavService
    {
        Task<byte[]> GetAsync(string path, CancellationToken cancellationToken = default);
        void MkCol(string path);
        Task<string> PropfindAsync(PropfindRequest r, CancellationToken cancellationToken = default);
        void Delete(string path);
        void Move(MoveRequest r);
        void Copy(CopyRequest r);
        Task PutAsync(string path, byte[] data, CancellationToken cancellationToken = default);
        LockResponse Lock(LockRequest r);
        void Unlock(string path);
    }
}
