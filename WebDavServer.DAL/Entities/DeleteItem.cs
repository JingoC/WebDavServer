using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebDavServer.FileStorage.Enums;

namespace WebDavServer.FileStorage.Entities
{
    [Table("DeleteItems")]
    public class DeleteItem
    {
        [Key]
        public string OriginalName { get; set; }
        public string OriginalPath { get; set; }
        public string CurrentName { get; set; }
        public string CurrentPath { get; set; }
        public ItemType Type { get; set; }
    }
}
