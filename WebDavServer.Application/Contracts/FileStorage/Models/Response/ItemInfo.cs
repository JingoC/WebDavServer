using WebDavServer.Application.Contracts.FileStorage.Enums;

namespace WebDavServer.Application.Contracts.FileStorage.Models.Response
{
    public class ItemInfo
    {
        public ItemType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; } = 0;
        public string CreatedDate { get; set; } = string.Empty;
        public string ModifyDate { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public bool IsRoot { get; set; } = false;
        public bool IsExists { get; set; } = false;
        public bool IsForbidden { get; set; } = false;
    }
}
