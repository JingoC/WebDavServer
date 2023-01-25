namespace WebDavServer.Application.Contracts.WebDav.Models.Request
{
    public class MoveRequest
    {
        public string SrcPath { get; init; } = null!;
        public string DstPath { get; init; } = null!;
        public bool IsForce { get; set; }
    }
}
