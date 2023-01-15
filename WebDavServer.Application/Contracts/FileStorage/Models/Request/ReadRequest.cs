namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    /// <summary>
    /// Read data request model
    /// </summary>
    public class ReadRequest
    {
        /// <summary>
        /// Path to read data resource
        /// </summary>
        public string Path { get; init; } = null!;
    }
}
