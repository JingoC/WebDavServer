namespace WebDavService.Application.Contracts.Cache
{
    public interface ICacheProvider
    {
        void Remove(string cacheKey);
        void Set<T>(string cacheKey, T cacheValue);
        T Get<T>(string cacheKey, int cacheTimeInMinutes, Func<T> func);
        Task<T> GetAsync<T>(string cacheKey, int cacheTimeInMinutes, Func<Task<T>> func);
    }
}
