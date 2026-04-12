namespace SleeperFunctions.Application;

public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl);
}