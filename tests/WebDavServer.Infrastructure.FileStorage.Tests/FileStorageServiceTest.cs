using Moq;
using WebDavServer.Application.Contracts.Cache;
using WebDavServer.Application.Contracts.FileStorage.Enums;
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

            var fileStorageService = new FileStorageService(new Mock<ICacheProvider>().Object,
                physicalStorageServiceMock.Object,
                virtualStorageService, new PathService(dbContext));

            var response = await fileStorageService.CreateAsync(FileStorageHelper.CreteDirectoryRequest("/dir"));

            Assert.Equal(ErrorType.None, response.ErrorType);

            var pathInfo = PathInfoHelper.GetDirectory("dir", 100, null, "root");
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
    }
}
