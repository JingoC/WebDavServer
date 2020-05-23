using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebDavServer.FileStorage.Enums;

namespace WebDavServer.FileStorage.Entities
{
    [Table("")]
    public class Item
    {
        [Key]
        public int Id { get; set; }
        public int? DataId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public ItemType Type { get; set; }
        public string LockTocken { get; set; }
    }
}
