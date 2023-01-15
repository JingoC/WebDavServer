namespace WebDavServer.Application.Contracts.FileStorage.Models.Response
{
    /// <summary>
    /// Read resource response model
    /// </summary>
    public class ReadResponse : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Stream from read
        /// </summary>
        public Stream ReadStream { get; init; } = null!;

        public static ReadResponse Create(Stream stream) => new() {ReadStream = stream};

        public void Dispose()
        {
            ReadStream.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await ReadStream.DisposeAsync();
        }
    }
}
