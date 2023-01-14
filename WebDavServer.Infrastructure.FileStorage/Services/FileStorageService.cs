using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebDavServer.Infrastructure.FileStorage.Options;
using WebDavService.Application.Contracts.Cache;
using WebDavService.Application.Contracts.FileStorage;
using WebDavService.Application.Contracts.FileStorage.Enums;
using WebDavService.Application.Contracts.FileStorage.Models;

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
        public string LockItemAsync(string path, int timeoutMin)
        {
            var fullPath = GetPath(path);
            return _cacheProvider.Get($"Lock_{fullPath}", timeoutMin, () =>
            {
                return Guid.NewGuid().ToString();
            });
        }
        public void UnlockItem(string path)
        {
            var fullPath = GetPath(path);

            _cacheProvider.Remove($"Lock_{fullPath}");
        }
        public Task<List<DeleteItem>> GetItemsAsync(string path, CancellationToken cancellationToken = default)
        {
            // TODO: WTF, DeleteItem, implement method
            return Task.FromResult(new List<DeleteItem>());
        }
        public List<ItemInfo> GetProperties(string path, bool withDirectoryContent)
        {
            var result = new List<ItemInfo>();

            var pi = CheckPath(path);

            if (pi.ItemType == ItemType.File)
            {
                var fi = new FileInfo(pi.FullPath);
                if (fi.Exists)
                    result.Add(ConvertFileInfoToItemInfo(fi, true));
            }
            else if (pi.ItemType == ItemType.Directory)
            {
                var di = new DirectoryInfo(GetPath(path));
                if (di.Exists)
                    result.Add(ConvertDirectoryInfoToItemInfo(di, true));

                if (withDirectoryContent)
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

            return result;
        }

        public async Task<byte[]> GetContentAsync(string path, CancellationToken cancellationToken = default)
        {
            var c = CheckPath(path);

            return c.ItemType == ItemType.File ? await File.ReadAllBytesAsync(GetPath(path), cancellationToken) : new byte[]{};
        }

        public void CreateDirectory(string path)
        {
            var fullPath = GetPath(path);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }
        public async Task CreateFileAsync(string path, byte[] data, CancellationToken cancellationToken = default)
        {
            var fullPath = GetPath(path);

            await File.WriteAllBytesAsync(fullPath, data, cancellationToken);
        }
        public void Delete(string path)
        {
            var pi = CheckPath(path);

            if (pi.ItemType == ItemType.File)
                File.Delete(pi.FullPath);
            else if (pi.ItemType == ItemType.Directory)
                Directory.Delete(pi.FullPath, true);
        }
        public Task DeleteRecyclerAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
        public void Move(MoveRequest r)
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
        }

        public void Copy(CopyRequest r)
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
