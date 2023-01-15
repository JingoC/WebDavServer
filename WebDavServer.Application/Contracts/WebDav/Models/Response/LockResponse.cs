namespace WebDavServer.Application.Contracts.WebDav.Models.Response
{
    public class LockResponse
    {
        public string LockToken { get; set; } = null!;
        public string Xml { get; set; } = null!;
    }
}
