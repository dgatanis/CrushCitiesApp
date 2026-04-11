using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions;
public class PlayerFunctions(ILogger<PlayerFunctions> logger, IHttpClientFactory http, IMemoryCache cache)
{
    private readonly ILogger<PlayerFunctions> _logger = logger;
    private readonly HttpClient _http = http.CreateClient("SleeperClient");
    private readonly IMemoryCache _cache = cache;

    [Function("GetPlayersAsync")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "players/nfl")] HttpRequest req)
    {
        var cacheKey = "sleeper-players";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<Dictionary<string, PlayersModel>>("players/nfl");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(6));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }
}