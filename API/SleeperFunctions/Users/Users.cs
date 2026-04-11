using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions;

public class UserFunctions(ILogger<UserFunctions> logger, IHttpClientFactory http, IMemoryCache cache)
{
    private readonly ILogger<UserFunctions> _logger = logger;
    private readonly HttpClient _http = http.CreateClient("SleeperClient");
    private readonly IMemoryCache _cache = cache;


    [Function("GetUsersAsync")]
    public async Task<IActionResult> GetUsersAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/users")] HttpRequest req, string league_id)
    {
        var cacheKey = $"sleeper-users-{league_id}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<UsersModel>>($"league/{league_id}/users");
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }
}