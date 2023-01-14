namespace WebDavService.Application.Contracts.FileStorage.Models
{
    public class CopyRequest
    {
        public string SrcPath { get; set; } = null!;
        public string DstPath { get; set; } = null!;
    }
}
