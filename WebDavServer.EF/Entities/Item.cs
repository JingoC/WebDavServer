#pragma warning disable CS8618

namespace WebDavServer.EF.Entities
{
    public class Item
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public bool IsDirectory { get; set; }
        public long? DirectoryId { get; set; }
        public Item Directory { get; set; }
        public string? Path { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public long Size { get; set; }
    }
}
