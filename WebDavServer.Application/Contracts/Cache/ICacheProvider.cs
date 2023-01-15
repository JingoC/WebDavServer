namespace WebDavServer.Application.Contracts.Cache
{
    public interface ICacheProvider
    {
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task<T?> GetOrSetAsync<T>(string key, int lifeTimePerMinute, Func<CancellationToken, Task<T>> setFunction, CancellationToken cancellationToken = default);
    }
}
