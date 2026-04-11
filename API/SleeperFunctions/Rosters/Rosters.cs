using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions;

public class RosterFunctions(ILogger<RosterFunctions> logger, IHttpClientFactory http, IMemoryCache cache)
{
    private readonly ILogger<RosterFunctions> _logger = logger;
    private readonly HttpClient _http = http.CreateClient("SleeperClient");
    private readonly IMemoryCache _cache = cache;


    [Function("GetRostersAsync")]
    public async Task<IActionResult> GetRostersAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/rosters")] HttpRequest req, string league_id)
    {
        var cacheKey = $"sleeper-rosters-{league_id}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<RostersModel>>($"league/{league_id}/rosters");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(1));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }
        return new OkObjectResult(cachedData);
    }
}