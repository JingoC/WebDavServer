using WebDavService.Application.Contracts.FileStorage.Models;
using WebDavService.Application.Contracts.WebDav.Models;

namespace WebDavService.Application.Contracts.WebDav
{
    public interface IWebDavService
    {
        Task<byte[]> GetAsync(string drive, string path);
        void MkCol(string drive, string path);
        Task<string> PropfindAsync(PropfindRequest r);
        void Delete(string drive, string path);
        void Move(MoveRequest r);
        void Copy(CopyRequest r);
        Task PutAsync(string drive, string path, byte[] data);
        LockResponse Lock(LockRequest r);
        void Unlock(string drive, string path);
    }
}
