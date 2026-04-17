using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace SleeperFunctions.Application;

public sealed class CacheService(IMemoryCache cache, ILogger<CacheService> logger) : ICacheService
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CacheService> _logger = logger;


    /// <summary>
    /// Checks the provided key in cache and returns if found. If not, execute deferred function and await results.
    /// Function passed in is expected to make the API call if no cache is found.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">Name of the cache key</param>
    /// <param name="factory">Function to execute if cache missed</param>
    /// <param name="ttl">How long to keep the cache entry</param>
    /// <returns>Cached data or newly fetched data</returns>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            _logger.LogDebug("Cache Hit [{CacheKey}]", key);
            return cached;
        }

        _logger.LogDebug("Cache Miss [{CacheKey}]", key);
        var data = await factory();
        _cache.Set(key, data, ttl);

        return data;
    }
}