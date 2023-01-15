﻿namespace WebDavServer.Application.Contracts.FileStorage.Models.Response
{
    public class DeleteResponse : BaseResponse
    {
        public List<DeleteItem> Items { get; init; } = null!;
    }
}
