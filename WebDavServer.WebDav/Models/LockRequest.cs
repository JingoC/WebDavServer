namespace WebDavServer.WebDav.Models
{
    public class LockRequest
    {
        public string Url { get; set; }
        public string Drive { get; set; }
        public string Path { get; set; }
        public string Xml { get; set; }
        public int TimeoutSecond { get; set; }
    }
}
