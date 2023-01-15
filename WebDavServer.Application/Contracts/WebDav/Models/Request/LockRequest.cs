namespace WebDavServer.Application.Contracts.WebDav.Models.Request
{
    public class LockRequest
    {
        public string Url { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Xml { get; set; } = null!;
        public int TimeoutSecond { get; set; }
    }
}
