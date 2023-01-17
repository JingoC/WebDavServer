using WebDavServer.EF.Entities;

namespace WebDavServer.Infrastructure.FileStorage.Models
{
    public class PathInfo
    {
        public string ResourceName { get; init; } = null!;
        public bool IsDirectory { get; init; }
        public string VirtualPath { get; init; } = null!;
        public PathInfo? Child { get; init; }
        public Item? Directory { get; init; }
    }
}
