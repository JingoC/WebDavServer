using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebDavServer.FileStorage.Entities;
using WebDavServer.FileStorage.Enums;
using WebDavServer.FileStorage.Models;
using WebDavServer.FileStorage.Options;

namespace WebDavServer.FileStorage.Services
{
    public interface IFileStorageService
    {
        Task<bool> LockItem(string drive, string path);
        Task<bool> UnlockItem(string drive, string path);
        Task<List<Item>> GetItems(string drive, string path);
        List<ItemInfo> GetProperties(string drive, string path, bool withDirectoryContent);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageOptions _options;
        public FileStorageService(IOptions<FileStorageOptions> options)
        {
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.Path))
            {
                throw new OptionsValidationException("Path", typeof(string), new[] { "value is null or empty" });
            }
        }
        public async Task<bool> LockItem(string drive, string path)
        {
            return true;
        }
        public async Task<bool> UnlockItem(string drive, string path)
        {
            return true;
        }
        public async Task<List<Item>> GetItems(string drive, string path)
        {
            return new List<Item>();
        }
        public List<ItemInfo> GetProperties(string drive, string path, bool withDirectoryContent)
        {
            var result = new List<ItemInfo>();

            CheckPath(drive, path);

            var dirPath = GetPath(drive, path);

            var fi = new FileInfo(dirPath);
            if (fi.Exists)
                result.Add(ConvertFileInfoToItemInfo(fi, true));
            else
            {
                var di = new DirectoryInfo(GetPath(drive, path));
                if (di.Exists)
                    result.Add(ConvertDirectoryInfoToItemInfo(di, true));
                else
                    throw new FileNotFoundException();
            }
            
            if (withDirectoryContent)
            {
                foreach (var dir in Directory.GetDirectories(dirPath))
                {
                    var di = new DirectoryInfo(dir);
                    if (di.Exists)
                        result.Add(ConvertDirectoryInfoToItemInfo(di, false));
                }

                foreach (var file in Directory.GetFiles(dirPath))
                {
                    var f = new FileInfo(file);
                    if (f.Exists)
                        result.Add(ConvertFileInfoToItemInfo(f, false));
                }
            }

            return result;
        }

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

        void CheckPath(string drive, string path)
        {
            var gPath = GetPath(drive, path);

            if (!(File.Exists(gPath) || Directory.Exists(gPath)))
                throw new FileNotFoundException();
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
