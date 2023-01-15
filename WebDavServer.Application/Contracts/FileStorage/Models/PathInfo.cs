using WebDavServer.Application.Contracts.FileStorage.Enums;

namespace WebDavServer.Application.Contracts.FileStorage.Models
{
    public class PathInfo
    {
        public ItemType ItemType { get; set; }
        public string FullPath { get; set; } = null!;
    }
}
