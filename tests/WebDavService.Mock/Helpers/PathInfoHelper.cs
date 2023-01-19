using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Models;

namespace WebDavService.Mock.Helpers
{
    public static class PathInfoHelper
    {
        public static PathInfo GetFile(string resourceName, long directoryId, long parentDirectoryId, string titleDirectory)
            => new()
            {
                ResourceName = resourceName,
                IsDirectory = false,
                Directory = new Item
                {
                    Id = directoryId,
                    DirectoryId = parentDirectoryId,
                    IsDirectory = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    Title = titleDirectory
                }
            };

        public static PathInfo GetDirectory(string resourceName, long directoryId, long? parentDirectoryId, string titleDirectory)
            => new()
            {
                ResourceName = resourceName,
                IsDirectory = true,
                Directory = new Item
                {
                    Id = directoryId,
                    DirectoryId = parentDirectoryId,
                    IsDirectory = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    Title = titleDirectory
                }
            };
    }
}
