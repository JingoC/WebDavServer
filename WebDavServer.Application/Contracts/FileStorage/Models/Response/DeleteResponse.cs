namespace WebDavServer.Application.Contracts.FileStorage.Models.Response
{
    public class DeleteResponse
    {
        public List<DeleteItem> Items { get; init; } = null!;
    }
}
