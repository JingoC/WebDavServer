using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebDavServer.Application.Contracts.Cache;
using WebDavServer.Application.Contracts.FileStorage;
using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.FileStorage.Models;
using WebDavServer.Application.Contracts.FileStorage.Models.Request;
using WebDavServer.Application.Contracts.FileStorage.Models.Response;
using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Enums;
using WebDavServer.Infrastructure.FileStorage.Exceptions;
using WebDavServer.Infrastructure.FileStorage.Options;
using WebDavServer.Infrastructure.FileStorage.Services.Abstract;

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
        private readonly IPhysicalStorageService _physicalStorageService;
        private readonly IVirtualStorageService _virtualStorageService;
        private readonly IPathService _pathService;

        public FileStorageService(
            IOptions<FileStorageOptions> options,
            ICacheProvider cacheProvider, ILogger<FileStorageService> logger, IPhysicalStorageService physicalStorageService, IVirtualStorageService virtualStorageService, IPathService pathService)
        {
            _options = options.Value;
            _cacheProvider = cacheProvider;
            _logger = logger;
            _physicalStorageService = physicalStorageService;
            _virtualStorageService = virtualStorageService;
            _pathService = pathService;
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
                case ItemType.Directory: errorType = await CreateDirectoryAsync(request.Path, cancellationToken);
                break;
                case ItemType.File: errorType = await CreateFileAsync(request.Path, request.Stream!, cancellationToken);
                break;
            }

            return new CreateResponse
            {
                ErrorType = errorType
            };
        }

        public async Task<ReadResponse> ReadAsync(ReadRequest request, CancellationToken cancellationToken = default)
        {
            var pathInfo = await _pathService.GetDestinationPathInfoAsync(request.Path, cancellationToken);

            var item = await _virtualStorageService.GetFileInfoAsync(pathInfo, cancellationToken);

            if (item is null)
            {
                throw new FileStorageException(ErrorCodes.NotFound);
            }

            var stream = await _physicalStorageService.ReadFileAsync(item.Name, cancellationToken);
            
            return ReadResponse.Create(stream);
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

        public async Task<GetPropertiesResponse> GetPropertiesAsync(GetPropertiesRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var pathInfo = await _pathService.GetDestinationPathInfoAsync(request.Path, cancellationToken);

                return new GetPropertiesResponse
                {
                    Items = pathInfo.IsDirectory
                        ? await GetDirectoryPropertiesAsync(pathInfo, request.WithDirectoryContent, cancellationToken)
                        : await GetFilePropertiesAsync(pathInfo, cancellationToken)
                };
            }
            catch (FileStorageException e) when(e.ErrorCode == ErrorCodes.NotFound)
            {
                return new GetPropertiesResponse() {Items = new() { ConvertNotFoundToItemInfo(false, false, false) } };
            }
        }

        private async Task<List<ItemInfo>> GetFilePropertiesAsync(Models.PathInfo pathInfo, CancellationToken cancellationToken)
        {
            var result = new List<ItemInfo>();
            var fileInfo = await _virtualStorageService.GetFileInfoAsync(pathInfo, cancellationToken);

            // TODO: throw exception

            if (fileInfo is not null)
            {
                result.Add(ConvertFileInfoToItemInfo(fileInfo, true));
            }

            return result;
        }

        private async Task<List<ItemInfo>> GetDirectoryPropertiesAsync(Models.PathInfo pathInfo, bool isRecursive, CancellationToken cancellationToken)
        {
            var result = new List<ItemInfo>();

            var directoryInfoList = await _virtualStorageService.GetDirectoryInfoAsync(pathInfo, isRecursive, cancellationToken);

            var isRoot = true;

            foreach (var item in directoryInfoList)
            {
                result.Add(item.IsDirectory ? ConvertDirectoryInfoToItemInfo(item, isRoot)
                    : ConvertFileInfoToItemInfo(item, false));

                isRoot = false;
            }
            
            return result;
        }

        private async Task<ErrorType> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            var pathInfo = await _pathService.GetDestinationPathInfoAsync(path, cancellationToken);

            var isFileExists = await _virtualStorageService.FileExistsAsync(pathInfo, cancellationToken);

            if (isFileExists)
            {
                return ErrorType.ResourceExists;
            }
            
            await _virtualStorageService.CreateDirectoryAsync(pathInfo, cancellationToken);

            return ErrorType.None;
        }

        private async Task<ErrorType> CreateFileAsync(string path, Stream stream, CancellationToken cancellationToken = default)
        {
            var pathInfo = await _pathService.GetDestinationPathInfoAsync(path, cancellationToken);

            var isFileExists = await _virtualStorageService.FileExistsAsync(pathInfo, cancellationToken);

            if (isFileExists)
            {
                return ErrorType.ResourceExists;
            }

            var fileName = await _physicalStorageService.WriteFileAsync(stream, cancellationToken);
            
            await _virtualStorageService.CreateFileAsync(fileName, pathInfo, cancellationToken);

            return ErrorType.None;
        }
        
        ItemInfo ConvertFileInfoToItemInfo(Item item, bool isRoot, bool isExists = true, bool isForbidden = false)
        {
            return new ItemInfo()
            {
                CreatedDate = item.CreatedDate.ToString(),
                ModifyDate = item.UpdatedDate.ToString(),
                IsRoot = isRoot,
                Name = item.Title,
                Type = ItemType.File,
                Size = item.Size,
                ContentType = GetContentType(item.Title),
                IsExists = isExists,
                IsForbidden = isForbidden
            };
        }

        ItemInfo ConvertDirectoryInfoToItemInfo(Item item, bool isRoot, bool isExists = true, bool isForbidden = false)
        {
            return new ItemInfo
            {
                CreatedDate = item.CreatedDate.ToString(),
                ModifyDate = item.UpdatedDate.ToString(),
                IsRoot = isRoot,
                Name = item.Title,
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
