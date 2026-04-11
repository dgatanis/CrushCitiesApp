using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net.Http.Json;
using System.Text.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace SleeperFunctions;

public class LeagueFunctions(ILogger<LeagueFunctions> logger, IHttpClientFactory http, IConfiguration config, IMemoryCache cache)
{
    private readonly ILogger<LeagueFunctions> _logger = logger;
    private readonly HttpClient _http = http.CreateClient("SleeperClient");
    private readonly string _userId = config["SleeperUserId"]!;
    private readonly IMemoryCache _cache = cache;


    [Function("GetLeagueByIdAsync")]
    public async Task<IActionResult> GetLeagueByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}")] HttpRequest req, string league_id)
    {
        var cacheKey = $"sleeper-leagues-{league_id}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<LeagueModel>($"league/{league_id}");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(2));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }



    [Function("GetLeagueBySeasonAsync")]
    public async Task<IActionResult> GetLeagueBySeasonAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/leagues/nfl/{season}")] HttpRequest req, string season)
    {
        var cacheKey = $"sleeper-leagues-by-season-{season}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<LeagueModel>>($"user/{_userId}/leagues/nfl/{season}");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(2));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }


    [Function("GetNFLStateAsync")]
    public async Task<IActionResult> GetNFLStateAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "state/nfl")] HttpRequest req)
    {
        var cacheKey = "sleeper-nfl-state";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<NFLStateModel>($"state/nfl");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(10));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }

}