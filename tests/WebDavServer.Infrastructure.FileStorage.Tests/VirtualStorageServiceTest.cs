using AutoFixture.Xunit2;
using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Models;
using WebDavServer.Infrastructure.FileStorage.Services;
using WebDavService.Mock.ef;

namespace WebDavServer.Infrastructure.FileStorage.Tests
{
    public class VirtualStorageServiceTest
    {
        [Fact]
        public async Task FileExists()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();
            
            var pathInfo = new PathInfo()
            {
                IsDirectory = false,
                ResourceName = "test",
                Directory = new Item
                {
                    IsDirectory = true,
                    DirectoryId = 100,
                    Id = 2,
                    Title = "dir"
                }
            };

            var isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(pathInfo);

            Assert.False(isExists);

            dbContext.AddDirectory(2, "dir", 100);

            isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(pathInfo);

            Assert.False(isExists);

            dbContext.AddFile(3, "file", 2);

            isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(pathInfo);

            Assert.False(isExists);

            dbContext.AddFile(4, "test", 2);

            isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(pathInfo);

            Assert.True(isExists);
        }

        [Fact]
        public async Task DirectoryExists()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            var pathInfo = new PathInfo()
            {
                IsDirectory = true,
                ResourceName = "test",
                Directory = new Item
                {
                    IsDirectory = true,
                    DirectoryId = 100,
                    Id = 2,
                    Title = "dir"
                }
            };

            var isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);

            Assert.False(isExists);

            dbContext.AddDirectory(5, "test", 100);

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);

            Assert.False(isExists);

            dbContext.AddDirectory(3, "dir", 100);

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);

            Assert.False(isExists);

            dbContext.AddDirectory(2, "dir", 100);

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);

            Assert.False(isExists);

            dbContext.AddDirectory(4, "test", 2);

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);

            Assert.True(isExists);
        }

        [Theory, AutoData]
        public async Task CreateFile(string fileName, string fileTitle)
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            var pathInfo = new PathInfo()
            {
                IsDirectory = false,
                ResourceName = fileTitle,
                Directory = new Item
                {
                    Id = 2,
                    Title = PathService.RootDirectory,
                    IsDirectory = true,
                    DirectoryId = 100
                }
            };

            await new VirtualStorageService(dbContext)
                .CreateFileAsync(fileName, pathInfo);

            var isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(pathInfo);

            Assert.True(isExists);
        }

        [Theory, AutoData]
        public async Task CreateDirectory(string directoryTitle)
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            var pathInfo = new PathInfo()
            {
                IsDirectory = true,
                ResourceName = directoryTitle,
                Directory = new Item
                {
                    Id = 2,
                    Title = PathService.RootDirectory,
                    IsDirectory = true,
                    DirectoryId = 100
                }
            };

            await new VirtualStorageService(dbContext)
                .CreateDirectoryAsync(pathInfo);

            var isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);

            Assert.True(isExists);
        }

        [Theory, AutoData]
        public async Task GetFileInfo()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();
            dbContext.AddDirectory(1, "dir", 100)
                .AddFile(2, "test", 1);

            var pathInfo = new PathInfo
            {
                IsDirectory = false,
                ResourceName = "test",
                Directory = new Item
                {
                    Id = 1,
                    Title = "dir",
                    IsDirectory = true,
                    DirectoryId = 100
                }
            };

            var fileInfo = await new VirtualStorageService(dbContext)
                .GetFileInfoAsync(pathInfo);

            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.IsDirectory);
            Assert.Equal(1, fileInfo.DirectoryId);
            Assert.Equal(2, fileInfo.Id);
        }

        [Theory, AutoData]
        public async Task GetDirectoryInfo()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();
            dbContext.AddDirectory(1, "dir", 100)
                .AddDirectory(2, "dir2", 1)
                .AddFile(3, "test", 2)
                .AddFile(4, "test2", 1)
                ;

            var pathInfo = new PathInfo
            {
                IsDirectory = true,
                ResourceName = "dir2",
                Directory = new Item
                {
                    Id = 1,
                    Title = "dir",
                    IsDirectory = true,
                    DirectoryId = 100
                }
            };

            var directoryInfos = await new VirtualStorageService(dbContext)
                .GetDirectoryInfoAsync(pathInfo, false);

            var directoryInfo = directoryInfos.First();
            
            Assert.Single(directoryInfos);
            Assert.NotNull(directoryInfo);
            Assert.True(directoryInfo.IsDirectory);
            Assert.Equal(1, directoryInfo.DirectoryId);
            Assert.Equal(2, directoryInfo.Id);

            directoryInfos = await new VirtualStorageService(dbContext)
                .GetDirectoryInfoAsync(pathInfo, true);

            Assert.Equal(2, directoryInfos.Count);
            Assert.Single(directoryInfos, x => x.IsDirectory && x.DirectoryId == 1 && x.Id == 2);
            Assert.Single(directoryInfos, x => !x.IsDirectory && x.DirectoryId == 2 && x.Id == 3);
        }

        [Theory, AutoData]
        public async Task DeleteFile(string fileName)
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();
            dbContext.AddDirectory(1, "dir", 100)
                .AddFile(2, "test", 1, fileName);

            var pathInfo = new PathInfo
            {
                IsDirectory = false,
                ResourceName = "test",
                Directory = new Item
                {
                    Id = 1,
                    DirectoryId = 100,
                    IsDirectory = true,
                    Title = "dir"
                }
            };

            var isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(pathInfo);

            Assert.True(isExists);

            var deletedFile = await new VirtualStorageService(dbContext)
                .DeleteFileAsync(pathInfo);

            Assert.Equal(fileName, deletedFile);

            isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(pathInfo);

            Assert.False(isExists);
        }

        [Theory, AutoData]
        public async Task DeleteDirectory(string fileName1, string fileName2, string fileName3)
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();
            dbContext.AddDirectory(1, "dir", 100)
                .AddFile(5, "test3", 1, fileName3)
                .AddDirectory(2, "dir2", 1)
                .AddFile(3, "test1", 2, fileName1)
                .AddFile(4, "test2", 2, fileName2);

            var pathInfo = new PathInfo
            {
                IsDirectory = true,
                ResourceName = "dir2",
                Directory = new Item
                {
                    Id = 1,
                    DirectoryId = 100,
                    IsDirectory = true,
                    Title = "dir"
                }
            };

            var isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);
            
            Assert.True(isExists);

            var deletedFiles = await new VirtualStorageService(dbContext)
                .DeleteDirectoryAsync(pathInfo);

            Assert.Equal(2, deletedFiles.Count);
            Assert.Single(deletedFiles, x => x.Equals(fileName1));
            Assert.Single(deletedFiles, x => x.Equals(fileName2));

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(pathInfo);

            Assert.False(isExists);
        }
    }
}
