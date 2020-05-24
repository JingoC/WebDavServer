namespace WebDavServer.WebDav.Models
{
    public class LockInfo
    {
        public string LockScope { get; set; }
        public string LockType { get; set; }
        public string Owner { get; set; }
    }
}
