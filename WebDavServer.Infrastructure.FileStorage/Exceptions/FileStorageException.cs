using WebDavServer.Infrastructure.FileStorage.Enums;

namespace WebDavServer.Infrastructure.FileStorage.Exceptions
{
    public class FileStorageException : Exception
    {
        public ErrorCodes ErrorCode { get; }

        public FileStorageException(ErrorCodes errorCode, string? message = null) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
