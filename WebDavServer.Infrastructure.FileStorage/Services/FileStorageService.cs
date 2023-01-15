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
            var errorType = ErrorType.None;

            switch (request.ItemType)
            {
                case ItemType.Directory: errorType = CreateDirectory(request.Path);
                break;
                case ItemType.File: errorType = await CreateFileAsync(request.Path, request.Stream, cancellationToken);
                break;
            }

            return new CreateResponse
            {
                ErrorType = errorType
            };
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

        public Task<MoveResponse> MoveAsync(MoveRequest r, CancellationToken cancellationToken = default)
        {
            var errorType = ErrorType.None;
            var src = CheckPath(r.SrcPath);
            var dst = GetPath(r.DstPath);

            if (src.ItemType == ItemType.File)
            {
                if (File.Exists(dst))
                {
                    errorType = ErrorType.ResourceExists;
                }
                else
                {
                    File.Move(src.FullPath, dst);
                }
            }
            else if (src.ItemType == ItemType.Directory)
            {
                Directory.Move(src.FullPath, dst);
            }

            return Task.FromResult(new MoveResponse
            {
                ErrorType = errorType
            });
        }

        public Task<CopyResponse> CopyAsync(CopyRequest r, CancellationToken cancellationToken = default)
        {
            var errorType = ErrorType.None;
            var src = CheckPath(r.SrcPath);
            var dst = GetPath(r.DstPath);
            
            if (src.ItemType == ItemType.File)
            {
                var isExists = File.Exists(dst);
                
                if (isExists)
                {
                    if (r.IsForce)
                    {
                        errorType = ErrorType.ResourceExists;
                    }
                    else
                    {
                        File.Delete(dst);
                        isExists = false;
                    }
                }

                if (!isExists)
                {
                    File.Copy(src.FullPath, dst);
                }
            }
            else if (src.ItemType == ItemType.Directory)
            {
                var isExists = Directory.Exists(dst);

                if (isExists)
                {
                    if (r.IsForce)
                    {
                        errorType = ErrorType.ResourceExists;
                    }
                    else
                    {
                        Directory.Delete(dst);
                        isExists = false;
                    }
                }

                if (!isExists)
                {
                    CopyDirectory(src.FullPath, dst, true);
                }
            }

            return Task.FromResult(new CopyResponse
            {
                ErrorType = errorType
            });
        }

        public Task<DeleteResponse> DeleteAsync(DeleteRequest request, CancellationToken cancellationToken = default)
        {
            var errorType = ErrorType.None;
            var pi = CheckPath(request.Path);

            if (pi.ItemType == ItemType.File)
            {
                File.Delete(pi.FullPath);
            }
            else if (pi.ItemType == ItemType.Directory)
            {
                Directory.Delete(pi.FullPath, true);
            }
            else if (pi.ItemType == ItemType.NotFound)
            {
                errorType = ErrorType.ResourceNotExists;
            }

            return Task.FromResult(new DeleteResponse()
            {
                Items = new List<DeleteItem>()
                {
                    new DeleteItem()
                    {
                        CurrentPath = pi.FullPath,
                        Type = pi.ItemType
                    }
                },
                ErrorType = errorType
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
        
        private ErrorType CreateDirectory(string path)
        {
            var fullPath = GetPath(path);

            if (Directory.Exists(fullPath))
            {
                return ErrorType.ResourceExists;
            }

            var partPath = Path.Combine(fullPath.Split(Path.DirectorySeparatorChar).SkipLast(1).ToArray());
            if (!Directory.Exists(partPath))
            {
                return ErrorType.PartResourcePathNotExists;
            }

            Directory.CreateDirectory(fullPath);

            return ErrorType.None;
        }

        private async Task<ErrorType> CreateFileAsync(string path, Stream stream, CancellationToken cancellationToken = default)
        {
            var fullPath = GetPath(path);

            await using var fileStream = File.Create(fullPath);
            
            await stream.CopyToAsync(fileStream, cancellationToken);

            return ErrorType.None;
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

            _logger.LogInformation($"[FS] Path: {fullPath}");
            
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

        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
    }
}
