using AutoFixture.Xunit2;
using WebDavServer.Infrastructure.FileStorage.Services;
using WebDavService.Mock.ef;
using WebDavService.Mock.Helpers;

namespace WebDavServer.Infrastructure.FileStorage.Tests
{
    public class VirtualStorageServiceTest
    {
        [Fact]
        public async Task FileExists()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();
            
            var pathInfo = PathInfoHelper.GetFile("test", 2, 100, "dir");
            
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

            var pathInfo = PathInfoHelper.GetDirectory("test", 2, 100, "dir");
            
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
            
            var pathInfo = PathInfoHelper.GetFile(fileTitle, 2, 100, PathService.RootDirectory);
            
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

            var pathInfo = PathInfoHelper.GetDirectory(directoryTitle, 2, 100, PathService.RootDirectory);
            
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

            var pathInfo = PathInfoHelper.GetFile("test", 1, 100, "dir");

            var fileInfo = await new VirtualStorageService(dbContext)
                .GetFileInfoAsync(pathInfo);

            Assert.NotNull(fileInfo);
            Assert.False(fileInfo.IsDirectory);
            Assert.Equal(1, fileInfo.DirectoryId);
            Assert.Equal(2, fileInfo.Id);
        }

        [Fact]
        public async Task GetDirectoryInfo()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();
            dbContext.AddDirectory(1, "dir", 100)
                .AddDirectory(2, "dir2", 1)
                .AddFile(3, "test", 2)
                .AddFile(4, "test2", 1)
                ;

            var pathInfo = PathInfoHelper.GetDirectory("dir2", 1, 100, "dir");
            
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

            var pathInfo = PathInfoHelper.GetFile("test", 1, 100, "dir");

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

            var pathInfo = PathInfoHelper.GetDirectory("dir2", 1, 100, "dir");
            
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

        [Fact]
        public async Task MoveFile()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            dbContext.AddDirectory(1, "dir", 100)
                .AddDirectory(2, "dir2", 100)
                .AddFile(3, "test", 1);
            
            var srcPathInfo = PathInfoHelper.GetFile("test", 1, 100, "dir");
            var dstPathInfo = PathInfoHelper.GetFile("test", 2, 100, "dir2");

            var isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(srcPathInfo);

            Assert.True(isExists);

            isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(dstPathInfo);

            Assert.False(isExists);

            await new VirtualStorageService(dbContext)
                .MoveFileAsync(srcPathInfo, dstPathInfo);

            isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(srcPathInfo);

            Assert.False(isExists);

            isExists = await new VirtualStorageService(dbContext)
                .FileExistsAsync(dstPathInfo);

            Assert.True(isExists);
        }

        [Fact]
        public async Task MoveDirectory()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            dbContext.AddDirectory(1, "dir", 100)
                .AddDirectory(2, "dir2", 100)
                .AddDirectory(3, "dir3", 1)
                .AddFile(4, "test", 3);

            var srcPathInfo = PathInfoHelper.GetDirectory("dir3", 1, 100, "dir");
            var dstPathInfo = PathInfoHelper.GetDirectory("dir7", 2, 100, "dir2");

            var isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(srcPathInfo);

            Assert.True(isExists);

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(dstPathInfo);

            Assert.False(isExists);
            
            await new VirtualStorageService(dbContext)
                .MoveDirectoryAsync(srcPathInfo, dstPathInfo);

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(srcPathInfo);

            Assert.False(isExists);

            isExists = await new VirtualStorageService(dbContext)
                .DirectoryExistsAsync(dstPathInfo);

            Assert.True(isExists);
        }
    }
}
