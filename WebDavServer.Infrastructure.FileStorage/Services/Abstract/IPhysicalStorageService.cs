using System.IO.Enumeration;

namespace WebDavServer.Infrastructure.FileStorage.Services.Abstract
{
    public interface IPhysicalStorageService
    {
        Task<string> WriteFileAsync(Stream stream, CancellationToken cancellationToken = default);
        Task<Stream> ReadFileAsync(string fileName, CancellationToken cancellationToken = default);
        Task<string> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);
    }
}
