namespace WebDavService.Application.Contracts.WebDav.Models
{
    public class LockResponse
    {
        public string LockToken { get; set; } = null!;
        public string Xml { get; set; } = null!;
    }
}
