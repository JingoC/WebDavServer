using WebDavServer.Application.Contracts.FileStorage.Enums;
using WebDavServer.Application.Contracts.FileStorage.Models.Request;

namespace WebDavService.Mock.Helpers
{
    public static class FileStorageHelper
    {
        public static CreateRequest CreteFileRequest(string path, Stream stream) => new() {ItemType = ItemType.File, Path = path, Stream = stream};
        public static CreateRequest CreteDirectoryRequest(string path) => new() { ItemType = ItemType.Directory, Path = path };
    }
}
