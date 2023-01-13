namespace WebDavService.Application.Contracts.FileStorage.Models
{
    public class MoveRequest
    {
        public string SrcPath { get; set; } = null!;
        public string DstPath { get; set; } = null!;
    }
}
