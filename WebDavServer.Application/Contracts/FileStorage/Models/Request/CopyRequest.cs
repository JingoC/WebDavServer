namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    public class CopyRequest
    {
        public string SrcPath { get; set; } = null!;
        public string DstPath { get; set; } = null!;
    }
}
