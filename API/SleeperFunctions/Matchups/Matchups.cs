using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions;

public class MatchupFunctions(ILogger<MatchupFunctions> logger, IHttpClientFactory http, IMemoryCache cache)
{
    private readonly ILogger<MatchupFunctions> _logger = logger;
    private readonly HttpClient _http = http.CreateClient("SleeperClient");
    private readonly IMemoryCache _cache = cache;


    [Function("GetMatchupsAsync")]
    public async Task<IActionResult> GetMatchupsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/matchups/{week}")] HttpRequest req, string league_id, string week)
    {
        var cacheKey = $"sleeper-matchups-{league_id}-{week}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<MatchupModel>>($"league/{league_id}/matchups/{week}");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(10));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }


    [Function("GetPlayoffWinnersAsync")]
    public async Task<IActionResult> GetPlayoffWinnersAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/winners_bracket")] HttpRequest req, string league_id)
    {
        var cacheKey = $"sleeper-winners-bracket-{league_id}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<PlayoffBracketsModel>>($"league/{league_id}/winners_bracket");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(6));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }


    [Function("GetPlayoffLosersAsync")]
    public async Task<IActionResult> GetPlayoffLosersAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/losers_bracket")] HttpRequest req, string league_id)
    {
        var cacheKey = $"sleeper-losers-bracket-{league_id}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<PlayoffBracketsModel>>($"league/{league_id}/losers_bracket");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(6));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }
}