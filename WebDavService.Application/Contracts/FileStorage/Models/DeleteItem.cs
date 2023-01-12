using WebDavService.Application.Contracts.FileStorage.Enums;

namespace WebDavService.Application.Contracts.FileStorage.Models
{
    public class DeleteItem
    {
        public string OriginalName { get; set; } = null!;
        public string OriginalPath { get; set; } = null!;
        public string CurrentName { get; set; } = null!;
        public string CurrentPath { get; set; } = null!;
        public ItemType Type { get; set; }
    }
}
