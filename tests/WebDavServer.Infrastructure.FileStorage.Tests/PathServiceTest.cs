using WebDavServer.Infrastructure.FileStorage.Enums;
using WebDavServer.Infrastructure.FileStorage.Exceptions;
using WebDavServer.Infrastructure.FileStorage.Services;
using WebDavService.Mock.ef;

namespace WebDavServer.Infrastructure.FileStorage.Tests
{
    public class PathServiceTest
    {
        [Fact]
        public async Task GetDistinationPathTest()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            dbContext.AddDirectory(1, "testdir1", 100)
                .AddDirectory(2, "testdir2_1", 1)
                .AddDirectory(3, "testdir2_2", 1)
                .AddDirectory(4, "testdir3_2_1_1", 2)
                .AddDirectory(5, "testdir3_2_1_2", 2)
                
                .AddFile(6, "testfile_root")
                .AddFile(7, "testfile1", 1)
                .AddFile(8, "testdir2_1", 1)
                .AddFile(9, "testdir2_1", 2)
                .AddFile(10, "testfile2_1", 2)
                .AddFile(11, "testfile3_2_1", 4)
                ;

            // 1 level directory
            var pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testdir1/");

            Assert.True(pathInfo.IsDirectory);
            Assert.Equal("testdir1", pathInfo.ResourceName);
            Assert.Equal("/root/", pathInfo.VirtualPath);
            
            Assert.NotNull(pathInfo.Directory);
            Assert.Equal(100, pathInfo.Directory.Id);

            // 2 level directory
            pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testdir1/testdir2_1/");

            Assert.True(pathInfo.IsDirectory);
            Assert.Equal("testdir2_1", pathInfo.ResourceName);
            Assert.Equal("/root/testdir1/", pathInfo.VirtualPath);

            Assert.NotNull(pathInfo.Directory);
            Assert.Equal("testdir1", pathInfo.Directory!.Title);
            Assert.True(pathInfo.Directory.IsDirectory);

            // 3 level directory
            pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testdir1/testdir2_1/testdir3_2_1_1/");

            Assert.True(pathInfo.IsDirectory);
            Assert.Equal("testdir3_2_1_1", pathInfo.ResourceName);
            Assert.Equal("/root/testdir1/testdir2_1/", pathInfo.VirtualPath);

            Assert.NotNull(pathInfo.Directory);
            Assert.Equal("testdir2_1", pathInfo.Directory!.Title);
            Assert.True(pathInfo.Directory.IsDirectory);

            // 1 level file
            pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testfile_root");

            Assert.False(pathInfo.IsDirectory);
            Assert.Equal("testfile_root", pathInfo.ResourceName);
            Assert.Equal("/root/", pathInfo.VirtualPath);

            Assert.NotNull(pathInfo.Directory);
            Assert.Equal(100, pathInfo.Directory.Id);

            // 2 level file
            pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testdir1/testfile1");

            Assert.False(pathInfo.IsDirectory);
            Assert.Equal("testfile1", pathInfo.ResourceName);
            Assert.Equal("/root/testdir1/", pathInfo.VirtualPath);

            Assert.NotNull(pathInfo.Directory);
            Assert.Equal("testdir1", pathInfo.Directory!.Title);
            Assert.Equal(1, pathInfo.Directory!.Id);
            Assert.Equal(100, pathInfo.Directory.DirectoryId);
            Assert.True(pathInfo.Directory.IsDirectory);

            // 3 level file
            pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testdir1/testdir2_1/testfile2_1");

            Assert.False(pathInfo.IsDirectory);
            Assert.Equal("testfile2_1", pathInfo.ResourceName);
            Assert.Equal("/root/testdir1/testdir2_1/", pathInfo.VirtualPath);

            Assert.NotNull(pathInfo.Directory);
            Assert.Equal("testdir2_1", pathInfo.Directory!.Title);
            Assert.Equal(2, pathInfo.Directory.Id);
            Assert.Equal(1, pathInfo.Directory.DirectoryId);
            Assert.True(pathInfo.Directory.IsDirectory);

            // file and directory same name, check directory
            pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testdir1/testdir2_1/");

            Assert.True(pathInfo.IsDirectory);
            Assert.Equal("testdir2_1", pathInfo.ResourceName);
            Assert.Equal("/root/testdir1/", pathInfo.VirtualPath);

            Assert.NotNull(pathInfo.Directory);
            Assert.Equal("testdir1", pathInfo.Directory!.Title);
            Assert.Equal(1, pathInfo.Directory.Id);
            Assert.Equal(100, pathInfo.Directory.DirectoryId);
            Assert.True(pathInfo.Directory.IsDirectory);

            // file and directory same name, check file
            pathInfo = await new PathService(dbContext)
                .GetDestinationPathInfoAsync("/testdir1/testdir2_1");

            Assert.False(pathInfo.IsDirectory);
            Assert.Equal("testdir2_1", pathInfo.ResourceName);
            Assert.Equal("/root/testdir1/", pathInfo.VirtualPath);

            Assert.NotNull(pathInfo.Directory);
            Assert.Equal("testdir1", pathInfo.Directory!.Title);
            Assert.Equal(1, pathInfo.Directory.Id);
            Assert.Equal(100, pathInfo.Directory.DirectoryId);
            Assert.True(pathInfo.Directory.IsDirectory);

            // part directory not exists, check directory
            var e = await Assert.ThrowsAsync<FileStorageException>(async () =>
                await new PathService(dbContext).GetDestinationPathInfoAsync("/testdir1/testdir2/testdir3_2_1_1/"));
            
            Assert.Equal(ErrorCodes.PartOfPathNotExists, e.ErrorCode);

            // part directory not exists, check file
            e = await Assert.ThrowsAsync<FileStorageException>(async () =>
                await new PathService(dbContext).GetDestinationPathInfoAsync("/testdir1/testdir2/testfile2_1"));

            Assert.Equal(ErrorCodes.PartOfPathNotExists, e.ErrorCode);
        }
    }
}