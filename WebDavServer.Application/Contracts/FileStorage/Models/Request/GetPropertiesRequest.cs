namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    /// <summary>
    /// Get resource properties request model
    /// </summary>
    public class GetPropertiesRequest
    {
        /// <summary>
        /// Path to resource
        /// </summary>
        public string Path { get; init; } = null!;
        public bool WithDirectoryContent { get; set; }
    }
}
