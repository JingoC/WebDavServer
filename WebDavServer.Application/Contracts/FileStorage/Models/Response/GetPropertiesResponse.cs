namespace WebDavServer.Application.Contracts.FileStorage.Models.Response
{
    public class GetPropertiesResponse
    {
        public List<ItemInfo> Items { get; init; } = null!;
    }
}
