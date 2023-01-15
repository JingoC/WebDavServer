using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebDavServer.Application.Contracts.Cache;
using WebDavServer.Application.Contracts.FileStorage;
using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.FileStorage.Models;
using WebDavServer.Application.Contracts.FileStorage.Models.Request;
using WebDavServer.Application.Contracts.FileStorage.Models.Response;
using WebDavServer.Infrastructure.FileStorage.Options;

namespace WebDavServer.Infrastructure.FileStorage.Services
{
    /// <summary>
    /// Class implementation File Storage
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageOptions _options;
        private readonly ICacheProvider _cacheProvider;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(
            IOptions<FileStorageOptions> options,
            ICacheProvider cacheProvider, ILogger<FileStorageService> logger)
        {
            _options = options.Value;
            _cacheProvider = cacheProvider;
            _logger = logger;
        }
        
        public async Task<LockResponse> LockAsync(LockRequest request, CancellationToken cancellationToken = default)
        {
            var fullPath = GetPath(request.Path);

            var lockToken = await _cacheProvider
                .GetOrSetAsync($"Lock_{fullPath}", request.TimeoutMin, 
                    (_) => Task.FromResult(Guid.NewGuid().ToString()), cancellationToken);

            return new LockResponse {Token = lockToken! };
        }

        public async Task UnlockAsync(UnlockRequest request, CancellationToken cancellationToken = default)
        {
            var fullPath = GetPath(request.Path);

            await _cacheProvider.RemoveAsync($"Lock_{fullPath}", cancellationToken);
        }

        public async Task<CreateResponse> CreateAsync(CreateRequest request, CancellationToken cancellationToken = default)
        {
            switch (request.ItemType)
            {
                case ItemType.Directory: CreateDirectory(request.Path);
                break;
                case ItemType.File: await CreateFileAsync(request.Path, request.Stream, cancellationToken);
                break;
            }

            return new CreateResponse();
        }

        public Task<ReadResponse> ReadAsync(ReadRequest request, CancellationToken cancellationToken = default)
        {
            var c = CheckPath(request.Path);

            if (c.ItemType != ItemType.File)
            {
                throw new InvalidOperationException("Read operation available only file");
            }

            var fullPath = GetPath(request.Path);

            return Task.FromResult(ReadResponse.Create(new StreamReader(fullPath).BaseStream));
        }

        public Task MoveAsync(MoveRequest r, CancellationToken cancellationToken = default)
        {
            var src = CheckPath(r.SrcPath);
            var dst = GetPath(r.DstPath);

            if (src.ItemType == ItemType.File)
            {
                File.Move(src.FullPath, dst);
            }
            else if (src.ItemType == ItemType.Directory)
            {
                Directory.Move(src.FullPath, dst);
            }

            return Task.CompletedTask;
        }

        public Task CopyAsync(CopyRequest r, CancellationToken cancellationToken = default)
        {
            var src = CheckPath(r.SrcPath);
            var dst = GetPath(r.DstPath);

            if (src.ItemType == ItemType.File)
            {
                File.Copy(src.FullPath, dst);
            }
            else if (src.ItemType == ItemType.Directory)
            {
                // TODO
            }

            return Task.CompletedTask;
        }

        public Task<DeleteResponse> DeleteAsync(DeleteRequest request, CancellationToken cancellationToken = default)
        {
            var pi = CheckPath(request.Path);

            if (pi.ItemType == ItemType.File)
                File.Delete(pi.FullPath);
            else if (pi.ItemType == ItemType.Directory)
                Directory.Delete(pi.FullPath, true);

            return Task.FromResult(new DeleteResponse()
            {
                Items = new List<DeleteItem>()
                {
                    new DeleteItem()
                    {
                        CurrentPath = pi.FullPath,
                        Type = pi.ItemType
                    }
                }
            });
        }

        public Task<GetPropertiesResponse> GetPropertiesAsync(GetPropertiesRequest request, CancellationToken cancellationToken = default)
        {
            var result = new List<ItemInfo>();

            var pi = CheckPath(request.Path);

            if (pi.ItemType == ItemType.File)
            {
                var fi = new FileInfo(pi.FullPath);
                if (fi.Exists)
                    result.Add(ConvertFileInfoToItemInfo(fi, true));
            }
            else if (pi.ItemType == ItemType.Directory)
            {
                var di = new DirectoryInfo(GetPath(request.Path));
                if (di.Exists)
                    result.Add(ConvertDirectoryInfoToItemInfo(di, true));

                if (request.WithDirectoryContent)
                {
                    foreach (var dir in Directory.GetDirectories(pi.FullPath))
                    {
                        var d = new DirectoryInfo(dir);
                        if (d.Exists)
                            result.Add(ConvertDirectoryInfoToItemInfo(d, false));
                    }

                    foreach (var file in Directory.GetFiles(pi.FullPath))
                    {
                        var f = new FileInfo(file);
                        if (f.Exists)
                            result.Add(ConvertFileInfoToItemInfo(f, false));
                    }
                }
            }
            else if (pi.ItemType == ItemType.NotFound)
                result.Add(ConvertNotFoundToItemInfo(false, false, false));

            return Task.FromResult(new GetPropertiesResponse()
            {
                Items = result
            });
        }
        
        private void CreateDirectory(string path)
        {
            var fullPath = GetPath(path);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }

        private async Task CreateFileAsync(string path, Stream stream, CancellationToken cancellationToken = default)
        {
            var fullPath = GetPath(path);

            using (var fileStream = File.Create(fullPath))
            {
                stream.Seek(0, SeekOrigin.Begin);
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
        }
        
        ItemInfo ConvertFileInfoToItemInfo(FileInfo fi, bool isRoot, bool isExists = true, bool isForbidden = false)
        {
            return new ItemInfo()
            {
                CreatedDate = fi.CreationTime.ToString(),
                ModifyDate = fi.LastWriteTime.ToString(),
                IsRoot = isRoot,
                Name = fi.Name,
                Type = ItemType.File,
                Size = fi.Length,
                ContentType = GetContentType(fi.Name),
                IsExists = isExists,
                IsForbidden = isForbidden
            };
        }

        ItemInfo ConvertDirectoryInfoToItemInfo(DirectoryInfo di, bool isRoot, bool isExists = true, bool isForbidden = false)
        {
            return new ItemInfo()
            {
                CreatedDate = di.CreationTime.ToString(),
                ModifyDate = di.LastWriteTime.ToString(),
                IsRoot = isRoot,
                Name = di.Name,
                Type = ItemType.Directory,
                IsExists = isExists,
                IsForbidden = isForbidden
            };
        }

        ItemInfo ConvertNotFoundToItemInfo(bool isRoot, bool isExists = true, bool isForbidden = false)
        {
            return new ItemInfo()
            {
                CreatedDate = DateTime.Now.ToString(),
                ModifyDate = DateTime.Now.ToString(),
                IsRoot = isRoot,
                Name = "Notfound",
                Type = ItemType.Directory,
                IsExists = isExists,
                IsForbidden = isForbidden
            };
        }

        string GetPath(string path)
        {
            var pathParts = new List<string>() {_options.Path};
            pathParts.AddRange(path.Split("/").ToArray());
            
            var fullPath = Path.Combine(pathParts.ToArray());

            _logger.LogInformation($"Path: {fullPath}");
            
            return fullPath;
        }

        PathInfo CheckPath(string path)
        {
            var result = new PathInfo()
            {
                FullPath = GetPath(path)
            };

            if (File.Exists(result.FullPath))
                result.ItemType = ItemType.File;
            else if (Directory.Exists(result.FullPath))
                result.ItemType = ItemType.Directory;
            else
                result.ItemType = ItemType.NotFound;
            
            return result;
        }

        private string GetContentType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();

            if (provider.TryGetContentType(fileName, out string contentType))
                return contentType;

            return "text/plain";
        }
    }
}
