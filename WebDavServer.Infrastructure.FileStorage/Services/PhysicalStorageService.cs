using Microsoft.Extensions.Options;
using WebDavServer.Infrastructure.FileStorage.Enums;
using WebDavServer.Infrastructure.FileStorage.Exceptions;
using WebDavServer.Infrastructure.FileStorage.Options;
using WebDavServer.Infrastructure.FileStorage.Services.Abstract;

namespace WebDavServer.Infrastructure.FileStorage.Services
{
    internal class PhysicalStorageService : IPhysicalStorageService
    {
        private readonly FileStorageOptions _options;

        public PhysicalStorageService(IOptionsSnapshot<FileStorageOptions> options)
        {
            _options = options.Value;
        }

        public async Task<string> WriteFileAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var fileName = Guid.NewGuid().ToString();

            var fullPath = GetPhysicalPath(fileName);

            await using var fileStream = File.Create(fullPath);

            await stream.CopyToAsync(fileStream, cancellationToken);
            
            return fileName;
        }

        public Task<string> CopyFileAsync(string srcFileName, CancellationToken cancellationToken = default)
        {
            var dstFileName = Guid.NewGuid().ToString();

            var srcFullPath = GetPhysicalPath(srcFileName);
            var dstFullPath = GetPhysicalPath(dstFileName);

            File.Copy(srcFullPath, dstFullPath);

            return Task.FromResult(dstFileName);
        }

        public Task<string> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var fullPath = GetPhysicalPath(fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.FromResult(fileName);
        }

        public Task<Stream> ReadFileAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var fullPath = GetPhysicalPath(fileName);

            if (!File.Exists(fullPath))
            {
                throw new FileStorageException(ErrorCodes.NotFound);
            }

            return Task.FromResult((Stream) File.OpenRead(fullPath));
        }

        string GetPhysicalPath(string fileName) => Path.Combine(_options.Path, fileName);
    }
}
