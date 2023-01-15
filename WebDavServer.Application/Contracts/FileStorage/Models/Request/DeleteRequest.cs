namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    /// <summary>
    /// DeleteAsync resource request model
    /// </summary>
    public class DeleteRequest
    {
        public string Path { get; init; } = null!;
    }
}
