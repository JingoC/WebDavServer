using WebDavServer.Application.Contracts.FileStorage.Enums;

namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    /// <summary>
    /// Create resource request model
    /// </summary>
    public class CreateRequest
    {
        /// <summary>
        /// Path to resource
        /// </summary>
        public string Path { get; init; } = null!;

        /// <summary>
        /// Data
        /// </summary>
        public Stream? Stream { get; init; }
        
        /// <summary>
        /// Type resource
        /// </summary>
        public ItemType ItemType { get; init; }
    }
}
