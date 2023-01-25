using AutoFixture.Xunit2;
using Moq;
using WebDavServer.Application.Contracts.Cache;
using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.FileStorage.Models.Request;
using WebDavServer.Infrastructure.FileStorage.Services;
using WebDavServer.Infrastructure.FileStorage.Services.Abstract;
using WebDavService.Mock.ef;
using WebDavService.Mock.Helpers;

namespace WebDavServer.Infrastructure.FileStorage.Tests
{
    public class FileStorageServiceTest
    {
        [Fact]
        public async Task CreateDirectory()
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            var physicalStorageServiceMock = new Mock<IPhysicalStorageService>();
            var virtualStorageService = new VirtualStorageService(dbContext);
            var pathService = new PathService(dbContext);

            var fileStorageService = new FileStorageService(new Mock<ICacheProvider>().Object,
                physicalStorageServiceMock.Object,
                virtualStorageService, pathService);

            var response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir"));

            Assert.Equal(ErrorType.None, response.ErrorType);

            var pathInfo = await pathService.GetDestinationPathInfoAsync("/dir");
            var isExists = await virtualStorageService.DirectoryExistsAsync(pathInfo);

            Assert.True(isExists);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dirиеу"));

            Assert.Equal(ErrorType.PartResourcePathNotExists, response.ErrorType);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir"));

            Assert.Equal(ErrorType.ResourceExists, response.ErrorType);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir1/dir2"));

            Assert.Equal(ErrorType.PartResourcePathNotExists, response.ErrorType);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir/dir1"));

            Assert.Equal(ErrorType.None, response.ErrorType);

            pathInfo = PathInfoHelper.GetDirectory("dir1", 1, 100, "dir");
            isExists = await virtualStorageService.DirectoryExistsAsync(pathInfo);

            Assert.True(isExists);
        }

        [Theory, AutoData]
        public async Task CreateFile(string fileName)
        {
            var dbContext = FileStoragePostgresDbContextMock.Create();

            var physicalStorageServiceMock = new Mock<IPhysicalStorageService>();
            physicalStorageServiceMock.Setup(x => x.WriteFileAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileName);
            var virtualStorageService = new VirtualStorageService(dbContext);
            var pathService = new PathService(dbContext);

            var fileStorageService = new FileStorageService(new Mock<ICacheProvider>().Object,
                physicalStorageServiceMock.Object,
                virtualStorageService, pathService);

            var response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir"));

            Assert.Equal(ErrorType.None, response.ErrorType);

            var pathInfo = await pathService.GetDestinationPathInfoAsync("/dir/test");
            var isExists = await virtualStorageService.FileExistsAsync(pathInfo);

            Assert.False(isExists);
            
            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteFileRequest("/dir/test", Stream.Null));

            Assert.Equal(ErrorType.None, response.ErrorType);
            
            isExists = await virtualStorageService.FileExistsAsync(pathInfo);
            Assert.True(isExists);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteFileRequest("/dir/test", Stream.Null));

            Assert.Equal(ErrorType.ResourceExists, response.ErrorType);
        }

        [Theory, AutoData]
        public async Task DeleteFile(string fileName)
        {
            var filePath = "/dir/test";

            var dbContext = FileStoragePostgresDbContextMock.Create();

            var physicalStorageServiceMock = new Mock<IPhysicalStorageService>();
            physicalStorageServiceMock.Setup(x => x.WriteFileAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileName);
            physicalStorageServiceMock.Setup(x => x.DeleteFileAsync(fileName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileName);

            var virtualStorageService = new VirtualStorageService(dbContext);
            var pathService = new PathService(dbContext);

            var fileStorageService = new FileStorageService(new Mock<ICacheProvider>().Object,
                physicalStorageServiceMock.Object,
                virtualStorageService, pathService);

            var response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir"));

            Assert.Equal(ErrorType.None, response.ErrorType);

            var pathInfo = await pathService.GetDestinationPathInfoAsync(filePath);
            var isExists = await virtualStorageService.FileExistsAsync(pathInfo);

            Assert.False(isExists);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteFileRequest(filePath, Stream.Null));

            Assert.Equal(ErrorType.None, response.ErrorType);
            
            isExists = await virtualStorageService.FileExistsAsync(pathInfo);
            Assert.True(isExists);

            var deleteResponse = await fileStorageService.DeleteAsync(new DeleteRequest {Path = filePath });
            
            Assert.Equal(ErrorType.None, response.ErrorType);
            Assert.Single(deleteResponse.Items);
        }

        [Theory, AutoData]
        public async Task DeleteDirectory(string fileName1, string fileName2)
        {
            var filePath1 = "/dir/test";
            var filePath2 = "/dir/test2";

            // mock

            var dbContext = FileStoragePostgresDbContextMock.Create();

            var physicalStorageServiceMock = new Mock<IPhysicalStorageService>();
            physicalStorageServiceMock
                .SetupSequence(x => x.WriteFileAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileName1)
                .ReturnsAsync(fileName2);
            
            physicalStorageServiceMock.Setup(x => x.DeleteFileAsync(fileName1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileName1);
            physicalStorageServiceMock.Setup(x => x.DeleteFileAsync(fileName2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileName2);

            var virtualStorageService = new VirtualStorageService(dbContext);
            var pathService = new PathService(dbContext);

            var fileStorageService = new FileStorageService(new Mock<ICacheProvider>().Object,
                physicalStorageServiceMock.Object,
                virtualStorageService, pathService);

            // preset

            var response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir"));

            Assert.Equal(ErrorType.None, response.ErrorType);

            var pathInfo1 = await pathService.GetDestinationPathInfoAsync(filePath1);
            var isExists = await virtualStorageService.FileExistsAsync(pathInfo1);

            Assert.False(isExists);

            var pathInfo2 = await pathService.GetDestinationPathInfoAsync(filePath2);
            isExists = await virtualStorageService.FileExistsAsync(pathInfo2);

            Assert.False(isExists);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteFileRequest(filePath1, Stream.Null));
            Assert.Equal(ErrorType.None, response.ErrorType);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteFileRequest(filePath2, Stream.Null));
            Assert.Equal(ErrorType.None, response.ErrorType);

            isExists = await virtualStorageService.FileExistsAsync(pathInfo1);
            Assert.True(isExists);

            isExists = await virtualStorageService.FileExistsAsync(pathInfo2);
            Assert.True(isExists);

            var deleteResponse = await fileStorageService.DeleteAsync(new DeleteRequest { Path = "/dir/" });

            physicalStorageServiceMock.Verify(x => x.DeleteFileAsync(fileName1, It.IsAny<CancellationToken>()), Times.Once);
            physicalStorageServiceMock.Verify(x => x.DeleteFileAsync(fileName2, It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(ErrorType.None, response.ErrorType);
        }

        [Theory, AutoData]
        public async Task MoveFile(string fileName)
        {
            var srcPath = "/dir/src/";
            var dstPath = "/dir/dst/";

            // mock

            var dbContext = FileStoragePostgresDbContextMock.Create();

            var physicalStorageServiceMock = new Mock<IPhysicalStorageService>();
            physicalStorageServiceMock.Setup(x => x.WriteFileAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileName);
            var virtualStorageService = new VirtualStorageService(dbContext);
            var pathService = new PathService(dbContext);

            var fileStorageService = new FileStorageService(new Mock<ICacheProvider>().Object,
                physicalStorageServiceMock.Object,
                virtualStorageService, pathService);

            // preset

            var response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir"));
            Assert.Equal(ErrorType.None, response.ErrorType);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir/src"));
            Assert.Equal(ErrorType.None, response.ErrorType);
            
            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir/dst"));
            Assert.Equal(ErrorType.None, response.ErrorType);

            
            var srcPathInfo = await pathService.GetDestinationPathInfoAsync(srcPath + "test");
            var isExists = await virtualStorageService.FileExistsAsync(srcPathInfo);

            Assert.False(isExists);

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteFileRequest(srcPath + "test", Stream.Null));

            Assert.Equal(ErrorType.None, response.ErrorType);

            isExists = await virtualStorageService.FileExistsAsync(srcPathInfo);
            Assert.True(isExists);

            // test

            var moveResponse = await fileStorageService.MoveAsync(new MoveRequest()
            {
                SrcPath = srcPath + "test",
                DstPath = dstPath
            });
            
            Assert.Equal(ErrorType.None, moveResponse.ErrorType);
            
            isExists = await virtualStorageService.FileExistsAsync(srcPathInfo);
            Assert.False(isExists);

            var dstPathInfo = await pathService.GetDestinationPathInfoAsync(dstPath + "test");
            isExists = await virtualStorageService.FileExistsAsync(dstPathInfo);

            Assert.True(isExists);

            // move srcPath not found

            moveResponse = await fileStorageService.MoveAsync(new MoveRequest()
            {
                SrcPath = srcPath + "test",
                DstPath = dstPath
            });

            Assert.Equal(ErrorType.ResourceNotExists, moveResponse.ErrorType);

            // move dstPath file exists

            response = await fileStorageService.CreateAsync(FileStorageHelper.CreteFileRequest(srcPath + "test", Stream.Null));
            Assert.Equal(ErrorType.None, response.ErrorType);

            isExists = await virtualStorageService.FileExistsAsync(srcPathInfo);
            Assert.True(isExists);

            moveResponse = await fileStorageService.MoveAsync(new MoveRequest()
            {
                SrcPath = srcPath + "test",
                DstPath = dstPath
            });

            Assert.Equal(ErrorType.ResourceExists, moveResponse.ErrorType);

            // move with new name

            moveResponse = await fileStorageService.MoveAsync(new MoveRequest()
            {
                SrcPath = srcPath + "test",
                DstPath = dstPath + "test2"
            });

            Assert.Equal(ErrorType.None, moveResponse.ErrorType);

            var dstPath2Info = await pathService.GetDestinationPathInfoAsync(dstPath + "test2");
            isExists = await virtualStorageService.FileExistsAsync(dstPath2Info);
            Assert.True(isExists);
        }
    }
}
