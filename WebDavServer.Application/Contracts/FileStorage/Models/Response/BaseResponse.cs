using WebDavServer.Application.Contracts.FileStorage.Enums;

namespace WebDavServer.Application.Contracts.FileStorage.Models.Response
{
    public class BaseResponse
    {
        public ErrorType ErrorType { get; set; } = ErrorType.None;
    }
}
