namespace WebDavService.Application.Contracts.FileStorage.Models
{
    public class CopyRequest
    {
        public string SrcDrive { get; set; } = null!;
        public string SrcPath { get; set; } = null!;
        public string DstDrive { get; set; } = null!;
        public string DstPath { get; set; } = null!;
    }
}
