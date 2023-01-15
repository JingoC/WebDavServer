using WebDavServer.Application.Contracts.WebDav.Enums;

namespace WebDavServer.Application.Contracts.WebDav.Models.Request
{
    public class PropfindRequest
    {
        public string Path { get; set; } = null!;
        public string Url { get; set; } = null!;
        public DepthType Depth { get; set; }
    }
}
