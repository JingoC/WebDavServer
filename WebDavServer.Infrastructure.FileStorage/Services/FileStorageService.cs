using Microsoft.AspNetCore.StaticFiles;
using System.Text.RegularExpressions;
using WebDavServer.Application.Contracts.Cache;
using WebDavServer.Application.Contracts.FileStorage;
using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.FileStorage.Models.Request;
using WebDavServer.Application.Contracts.FileStorage.Models.Response;
using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Enums;
using WebDavServer.Infrastructure.FileStorage.Exceptions;
using WebDavServer.Infrastructure.FileStorage.Services.Abstract;

namespace WebDavServer.Infrastructure.FileStorage.Services
{
    /// <summary>
    /// Class implementation File Storage
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly ICacheProvider _cacheProvider;
        private readonly IPhysicalStorageService _physicalStorageService;
        private readonly IVirtualStorageService _virtualStorageService;
        private readonly IPathService _pathService;

        public FileStorageService(
            ICacheProvider cacheProvider,
            IPhysicalStorageService physicalStorageService, 
            IVirtualStorageService virtualStorageService,
            IPathService pathService)
        {
            _cacheProvider = cacheProvider;
            _physicalStorageService = physicalStorageService;
            _virtualStorageService = virtualStorageService;
            _pathService = pathService;
        }
        
        public async Task<LockResponse> LockAsync(LockRequest request, CancellationToken cancellationToken = default)
        {
            // TODO: implementation lock 
            var fullPath = request.Path;

            var lockToken = await _cacheProvider
                .GetOrSetAsync($"Lock_{fullPath}", request.TimeoutMin, 
                    (_) => Task.FromResult(Guid.NewGuid().ToString()), cancellationToken);

            return new LockResponse {Token = lockToken! };
        }

        public async Task UnlockAsync(UnlockRequest request, CancellationToken cancellationToken = default)
        {
            // TODO: implementation unlock 
            var fullPath = request.Path;

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

        public async Task<MoveResponse> MoveAsync(MoveRequest r, CancellationToken cancellationToken = default)
        {
            var errorType = ErrorType.None;

            var srcPathInfo = await _pathService.GetDestinationPathInfoAsync(r.SrcPath, cancellationToken);
            var dstPathInfo = await _pathService.GetDestinationPathInfoAsync(r.DstPath, cancellationToken);

            if (srcPathInfo.IsDirectory)
            {
                var isExists = await _virtualStorageService.DirectoryExistsAsync(dstPathInfo, cancellationToken);

                if (isExists)
                {
                    if (r.IsForce)
                    {
                        var deleteFiles = await _virtualStorageService.DeleteDirectoryAsync(dstPathInfo, cancellationToken);
                        foreach (var deleteFile in deleteFiles)
                        {
                            await _physicalStorageService.DeleteFileAsync(deleteFile, cancellationToken);
                        }

                        isExists = false;
                    }

                    errorType = ErrorType.ResourceExists;
                }

                if (!isExists)
                {
                    await _virtualStorageService.MoveDirectoryAsync(srcPathInfo, dstPathInfo, cancellationToken);
                }
            }
            else
            {
                if (dstPathInfo.IsDirectory)
                {
                    dstPathInfo = await _pathService.GetDestinationPathInfoAsync($"{r.DstPath}{srcPathInfo.ResourceName}", cancellationToken);
                }

                var isSrcExists = await _virtualStorageService.FileExistsAsync(srcPathInfo, cancellationToken);
                if (!isSrcExists)
                {
                    errorType = ErrorType.ResourceNotExists;
                }
                else
                {
                    var isDstExists = await _virtualStorageService.FileExistsAsync(dstPathInfo, cancellationToken);

                    if (isDstExists)
                    {
                        if (r.IsForce)
                        {
                            var deleteFile = await _virtualStorageService.DeleteFileAsync(dstPathInfo, cancellationToken);
                            await _physicalStorageService.DeleteFileAsync(deleteFile, cancellationToken);

                            isDstExists = false;
                        }

                        errorType = ErrorType.ResourceExists;
                    }

                    if (!isDstExists)
                    {
                        try
                        {
                            await _virtualStorageService.MoveFileAsync(srcPathInfo, dstPathInfo, cancellationToken);
                        }
                        catch(FileStorageException e) when (e.ErrorCode == ErrorCodes.NotFound)
                        {
                            errorType = ErrorType.ResourceNotExists;
                        }
                    }
                }
            }
            
            return new MoveResponse
            {
                ErrorType = errorType
            };
        }

        public async Task<CopyResponse> CopyAsync(CopyRequest r, CancellationToken cancellationToken = default)
        {
            try
            {
                var errorType = ErrorType.None;

                var srcPathInfo = await _pathService.GetDestinationPathInfoAsync(r.SrcPath, cancellationToken);
                var dstPathInfo = await _pathService.GetDestinationPathInfoAsync(r.DstPath, cancellationToken);

                if (srcPathInfo.IsDirectory)
                {
                    var isExists = await _virtualStorageService.DirectoryExistsAsync(dstPathInfo, cancellationToken);

                    if (isExists)
                    {
                        if (r.IsForce)
                        {
                            var deleteFiles = await _virtualStorageService.DeleteDirectoryAsync(dstPathInfo, cancellationToken);
                            foreach (var deleteFile in deleteFiles)
                            {
                                await _physicalStorageService.DeleteFileAsync(deleteFile, cancellationToken);
                            }

                            isExists = false;
                        }

                        errorType = ErrorType.ResourceExists;
                    }

                    if (!isExists)
                    {
                        await _virtualStorageService.CopyDirectoryAsync(srcPathInfo, dstPathInfo, cancellationToken);
                    }
                }
                else
                {
                    var isExists = await _virtualStorageService.FileExistsAsync(dstPathInfo, cancellationToken);

                    if (isExists)
                    {
                        if (r.IsForce)
                        {
                            var deleteFile = await _virtualStorageService.DeleteFileAsync(dstPathInfo, cancellationToken);
                            await _physicalStorageService.DeleteFileAsync(deleteFile, cancellationToken);

                            isExists = false;
                        }

                        errorType = ErrorType.ResourceExists;
                    }

                    if (!isExists)
                    {
                        var fileInfo = await _virtualStorageService.GetFileInfoAsync(srcPathInfo, cancellationToken);

                        var copyFileName = await _physicalStorageService.CopyFileAsync(fileInfo!.Name, cancellationToken);
                        await _virtualStorageService.CopyFileAsync(srcPathInfo, dstPathInfo, copyFileName, cancellationToken);
                    }
                }

                return new CopyResponse
                {
                    ErrorType = errorType
                };
            }
            catch(FileStorageException e) when(e.ErrorCode == ErrorCodes.PartOfPathNotExists)
            {
                return new CopyResponse
                {
                    ErrorType = ErrorType.PartResourcePathNotExists
                };
            }
            catch (FileStorageException e) when (e.ErrorCode == ErrorCodes.NotFound)
            {
                return new CopyResponse
                {
                    ErrorType = ErrorType.ResourceNotExists
                };
            }
        }

        public async Task<DeleteResponse> DeleteAsync(DeleteRequest request, CancellationToken cancellationToken = default)
        {
            var errorType = ErrorType.None;

            try
            {
                var pathInfo = await _pathService.GetDestinationPathInfoAsync(request.Path, cancellationToken);

                if (pathInfo.IsDirectory)
                {
                    var files = await _virtualStorageService.DeleteDirectoryAsync(pathInfo, cancellationToken);

                    foreach (var file in files)
                    {
                        await _physicalStorageService.DeleteFileAsync(file, cancellationToken);
                    }
                }
                else
                {
                    var file = await _virtualStorageService.DeleteFileAsync(pathInfo, cancellationToken);
                    await _physicalStorageService.DeleteFileAsync(file, cancellationToken);
                }

                return new DeleteResponse
                {
                    Items = new List<DeleteItem>()
                    {
                        new DeleteItem
                        {
                            CurrentPath = pathInfo.VirtualPath,
                            Type = pathInfo.IsDirectory ? ItemType.Directory : ItemType.File
                        }
                    },
                    ErrorType = errorType
                };
            }
            catch (FileStorageException e) when(e.ErrorCode == ErrorCodes.PartOfPathNotExists || e.ErrorCode == ErrorCodes.NotFound)
            {
                return new DeleteResponse
                {
                    ErrorType = ErrorType.ResourceNotExists
                };
            }
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
            try
            {
                var pathInfo = await _pathService.GetDestinationPathInfoAsync(path, cancellationToken);

                if (!Regex.IsMatch(pathInfo.ResourceName, @"^[a-zA-Z0-9_]+$", RegexOptions.Compiled))
                {
                    return ErrorType.PartResourcePathNotExists;
                }

                var isFileExists = await _virtualStorageService.DirectoryExistsAsync(pathInfo, cancellationToken);

                if (isFileExists)
                {
                    return ErrorType.ResourceExists;
                }

                await _virtualStorageService.CreateDirectoryAsync(pathInfo, cancellationToken);

                return ErrorType.None;
            }
            catch (FileStorageException e) when(e.ErrorCode == ErrorCodes.PartOfPathNotExists)
            {
                return ErrorType.PartResourcePathNotExists;
            }
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
        
        private string GetContentType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();

            if (provider.TryGetContentType(fileName, out string contentType))
                return contentType;

            return "text/plain";
        }
    }
}
