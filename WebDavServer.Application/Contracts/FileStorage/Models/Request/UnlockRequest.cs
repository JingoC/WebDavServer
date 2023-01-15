namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    /// <summary>
    /// UnlockAsync resource request model
    /// </summary>
    public class UnlockRequest
    {
        /// <summary>
        /// Path to unlocking resource
        /// </summary>
        public string Path { get; init; } = null!;
    }
}
