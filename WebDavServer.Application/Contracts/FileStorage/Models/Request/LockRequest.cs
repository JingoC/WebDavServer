namespace WebDavServer.Application.Contracts.FileStorage.Models.Request
{
    /// <summary>
    /// LockAsync resource request model
    /// </summary>
    public class LockRequest
    {
        /// <summary>
        /// Path to locking resource
        /// </summary>
        public string Path { get; init; } = null!;

        /// <summary>
        /// Timeout lock per minute
        /// </summary>
        public int TimeoutMin { get; init; }
    }
}
