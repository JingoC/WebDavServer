using WebDavService.Application.Contracts.WebDav.Enums;

namespace WebDavService.Application.Contracts.WebDav.Models
{
    public class PropfindRequest
    {
        public string Path { get; set; } = null!;
        public string Url { get; set; } = null!;
        public DepthType Depth { get; set; }
        public string Xml { get; set; } = null!;
    }
}
