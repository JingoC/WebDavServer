namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    public class MoveRequest
    {
        public string SrcPath { get; set; } = null!;
        public string DstPath { get; set; } = null!;
    }
}
