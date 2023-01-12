using WebDavService.Application.Contracts.FileStorage.Enums;

namespace WebDavService.Application.Contracts.FileStorage.Models
{
    public class PathInfo
    {
        public ItemType ItemType { get; set; }
        public string FullPath { get; set; } = null!;
    }
}
