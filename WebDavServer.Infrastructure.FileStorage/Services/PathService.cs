using Microsoft.EntityFrameworkCore;
using WebDavServer.EF;
using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Enums;
using WebDavServer.Infrastructure.FileStorage.Exceptions;
using WebDavServer.Infrastructure.FileStorage.Models;
using WebDavServer.Infrastructure.FileStorage.Services.Abstract;

namespace WebDavServer.Infrastructure.FileStorage.Services
{
    public class PathService : IPathService
    {
        public const string RootDirectory = "root";

        private readonly FileStorageDbContext _dbContext;

        public PathService(FileStorageDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PathInfo> GetDestinationPathInfoAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            (string resourceName, List<string> directories, bool isSearchDirectory) = SplitPath(relativePath);
            
            PathInfo directory = null!;

            if (directories.Any())
            {
                var nextDirectory = directories[0];
                var otherDirectories = directories.Skip(1).ToList();

                var directoryInfo = await GetItemAsync(null, string.Empty, nextDirectory, otherDirectories, cancellationToken);

                directory = GetLastChild(directoryInfo!);
            }
            
            return new PathInfo
            {
                IsDirectory = isSearchDirectory,
                ResourceName = resourceName,
                Directory = directory.Directory,
                VirtualPath = directory.VirtualPath + "/"
            };
        }

        private (string resourceName, List<string> directories, bool isDirectory) SplitPath(string relativePath)
        {
            var relativePathTrim = relativePath.Trim();
            
            var directories = relativePathTrim.Split("/").Where(x => !string.IsNullOrEmpty(x)).ToList();

            var isDirectory = relativePathTrim == "/" || !Path.HasExtension(relativePathTrim);

            directories.Insert(0, RootDirectory);

            var resourceName = directories[directories.Count - 1];
            if (!isDirectory)
            {
                directories.RemoveAt(directories.Count - 1);
            }
            
            return (resourceName, directories, isDirectory);
        }

        private async Task<PathInfo?> GetItemAsync(
            long? parentDirectoryId,
            string relativePath,
            string directoryName, 
            List<string> nextDirectories,
            CancellationToken cancellationToken)
        {
            PathInfo? child = null;
            var item = await _dbContext.Set<Item>()
                .AsNoTracking()
                .Where(x => x.DirectoryId == parentDirectoryId)
                .Where(x => x.Title == directoryName)
                .Where(x => x.IsDirectory)
                .FirstOrDefaultAsync(cancellationToken);

            if (item == null)
            {
                return default;
            }

            var virtualPath = $"{relativePath}/{directoryName}";

            if (nextDirectories.Any())
            {
                var nextDirectory = nextDirectories[0];
                var otherDirectories = nextDirectories.Skip(1).ToList();

                child = await GetItemAsync(item.Id, virtualPath, nextDirectory, otherDirectories, cancellationToken);
            }

            return new PathInfo()
            {
                Child = child,
                IsDirectory = true,
                Directory = item,
                ResourceName = directoryName,
                VirtualPath = virtualPath
            };
        }

        PathInfo GetLastChild(PathInfo parent)
        {
            return parent.Child is null ? parent : GetLastChild(parent.Child);
        }
    }
}
