namespace WebDavServer.Application.Contracts.WebDav.Models
{
    public class LockInfo
    {
        public string LockScope { get; set; } = null!;
        public string LockType { get; set; } = null!;
        public string Owner { get; set; } = null!;
    }
}
