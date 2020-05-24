using WebDavServer.FileStorage.Enums;

namespace WebDavServer.FileStorage.Models
{
    public class PathInfo
    {
        public ItemType ItemType { get; set; }
        public string FullPath { get; set; }
    }
}
