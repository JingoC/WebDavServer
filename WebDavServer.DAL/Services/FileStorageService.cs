using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebDavServer.Core.Providers;
using WebDavServer.FileStorage.Entities;
using WebDavServer.FileStorage.Enums;
using WebDavServer.FileStorage.Models;
using WebDavServer.FileStorage.Options;

namespace WebDavServer.FileStorage.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Lock file method
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path to item</param>
        /// <param name="timeoutMin">Lock timeout per minute</param>
        /// <returns>Lock-Token</returns>
        string LockItemAsync(string drive, string path, int timeoutMin);
        /// <summary>
        /// UnLock file method
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path to item</param>
        void UnlockItem(string drive, string path);
        /// <summary>
        /// Get all items
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <returns></returns>
        Task<List<DeleteItem>> GetItemsAsync(string drive, string path);
        /// <summary>
        /// Get files\directories properties
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <param name="withDirectoryContent">Include files in current directory</param>
        /// <returns>List infoes</returns>
        List<ItemInfo> GetProperties(string drive, string path, bool withDirectoryContent);
        /// <summary>
        /// Get file data
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <returns>Data bytes</returns>
        Task<byte[]> GetContentAsync(string drive, string path);
        /// <summary>
        /// Create directory
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        void CreateDirectory(string drive, string path);
        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <param name="data">File data</param>
        /// <returns></returns>
        Task CreateFileAsync(string drive, string path, byte[] data);
        /// <summary>
        /// Delete file\directory
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        void Delete(string drive, string path);
        /// <summary>
        /// Delete from recycler
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <returns></returns>
        Task DeleteRecyclerAsync(string drive, string path);
        /// <summary>
        /// Move file\directory
        /// </summary>
        /// <param name="r">Move request</param>
        void Move(MoveRequest r);
        /// <summary>
        /// Copy file\directory
        /// </summary>
        /// <param name="r">Copy request</param>
        void Copy(CopyRequest r);
    }

    /// <summary>
    /// Class implementation File Storage
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageOptions _options;
        private readonly ICacheProvider _cacheProvider;
        public FileStorageService(
            IOptions<FileStorageOptions> options,
            ICacheProvider cacheProvider
            )
        {
            _options = options.Value;
            _cacheProvider = cacheProvider;
            if (string.IsNullOrWhiteSpace(_options.Path))
                throw new OptionsValidationException("Path", typeof(string), new[] { "value is null or empty" });
            if (string.IsNullOrWhiteSpace(_options.RecyclerPath))
                throw new OptionsValidationException("RecyclerPath", typeof(string), new[] { "value is null or empty" });
            if (string.IsNullOrWhiteSpace(_options.RecyclerName))
                throw new OptionsValidationException("RecyclerName", typeof(string), new[] { "value is null or empty" });
        }
        public string LockItemAsync(string drive, string path, int timeoutMin)
        {
            var fullPath = GetPath(drive, path);
            return _cacheProvider.Get($"Lock_{fullPath}", timeoutMin, () =>
            {
                return Guid.NewGuid().ToString();
            });
        }
        public void UnlockItem(string drive, string path)
        {
            var fullPath = GetPath(drive, path);

            _cacheProvider.Remove($"Lock_{fullPath}");
        }
        public async Task<List<DeleteItem>> GetItemsAsync(string drive, string path)
        {
            // TODO: WTF, DeleteItem, implement method
            return new List<DeleteItem>();
        }
        public List<ItemInfo> GetProperties(string drive, string path, bool withDirectoryContent)
        {
            var result = new List<ItemInfo>();

            var pi = CheckPath(drive, path);

            if (pi.ItemType == ItemType.File)
            {
                var fi = new FileInfo(pi.FullPath);
                if (fi.Exists)
                    result.Add(ConvertFileInfoToItemInfo(fi, true));
            }
            else if (pi.ItemType == ItemType.Directory)
            {
                var di = new DirectoryInfo(GetPath(drive, path));
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
            else
                throw new FileNotFoundException();

            return result;
        }

        public async Task<byte[]> GetContentAsync(string drive, string path)
        {
            CheckPath(drive, path);

            return await File.ReadAllBytesAsync(GetPath(drive, path));
        }

        public void CreateDirectory(string drive, string path)
        {
            var fullPath = GetPath(drive, path);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }
        public async Task CreateFileAsync(string drive, string path, byte[] data)
        {
            var fullPath = GetPath(drive, path);

            await File.WriteAllBytesAsync(fullPath, data);
        }
        public void Delete(string drive, string path)
        {
            var pi = CheckPath(drive, path);

            if (pi.ItemType == ItemType.File)
                File.Delete(pi.FullPath);
            else if (pi.ItemType == ItemType.Directory)
                Directory.Delete(pi.FullPath, true);
        }
        public async Task DeleteRecyclerAsync(string drive, string path)
        {

        }
        public void Move(MoveRequest r)
        {
            var src = CheckPath(r.SrcDrive, r.SrcPath);
            var dst = GetPath(r.DstDrive, r.DstPath);

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
            var src = CheckPath(r.SrcDrive, r.SrcPath);
            var dst = GetPath(r.DstDrive, r.DstPath);

            if (src.ItemType == ItemType.File)
            {
                File.Copy(src.FullPath, dst);
            }
            else if (src.ItemType == ItemType.Directory)
            {
                // TODO
            }
        }

        #region private_methods

        ItemInfo ConvertFileInfoToItemInfo(FileInfo fi, bool isRoot)
        {
            return new ItemInfo()
            {
                CreatedDate = fi.CreationTime.ToString(),
                ModifyDate = fi.LastWriteTime.ToString(),
                IsRoot = isRoot,
                Name = fi.Name,
                Type = ItemType.File,
                Size = fi.Length,
                ContentType = GetContentType(fi.Name)
            };
        }

        ItemInfo ConvertDirectoryInfoToItemInfo(DirectoryInfo di, bool isRoot)
        {
            return new ItemInfo()
            {
                CreatedDate = di.CreationTime.ToString(),
                ModifyDate = di.LastWriteTime.ToString(),
                IsRoot = isRoot,
                Name = di.Name,
                Type = ItemType.Directory
            };
        }

        string GetPath(string drive, string path)
        {
            return Path.Combine(_options.Path, drive, path);
        }

        PathInfo CheckPath(string drive, string path)
        {
            var result = new PathInfo()
            {
                FullPath = GetPath(drive, path)
            };

            if (File.Exists(result.FullPath))
                result.ItemType = ItemType.File;
            else if (Directory.Exists(result.FullPath))
                result.ItemType = ItemType.Directory;
            else
                throw new FileNotFoundException();

            return result;
        }

        private string GetContentType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();

            if (provider.TryGetContentType(fileName, out string contentType))
                return contentType;

            return "text/plain";
        }

        #endregion
    }
}
