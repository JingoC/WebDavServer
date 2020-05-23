using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebDavServer.FileStorage.Entities
{
    [Table("ItemData")]
    public class DataItem
    {
        [Key]
        public int Id { get; set; }
        public string Data { get; set; }
    }
}
