using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions;

public class DraftFunctions(ILogger<DraftFunctions> logger, IHttpClientFactory http, IMemoryCache cache)
{
    private readonly ILogger<DraftFunctions> _logger = logger;
    private readonly HttpClient _http = http.CreateClient("SleeperClient");
    private readonly IMemoryCache _cache = cache;

    [Function("GetDraftsAsync")]
    public async Task<IActionResult> GetDraftsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/drafts")] HttpRequest req, string league_id)
    {
        var cacheKey = $"sleeper-drafts-{league_id}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogInformation($"Cache miss [{cacheKey}] - fetching from Sleeper");
            cachedData = await _http.GetFromJsonAsync<List<DraftsModel>>($"league/{league_id}/drafts");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(2));
        }
        else
        {
            _logger.LogInformation($"Cache hit [{cacheKey}] - returning cached data");
        }

        return new OkObjectResult(cachedData);
    }


    [Function("GetDraftPicksAsync")]
    public async Task<IActionResult> GetDraftPicksAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "draft/{draft_id}/picks")] HttpRequest req, string draft_id)
    {
        var cacheKey = $"sleeper-draft-picks-{draft_id}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<DraftPicksModel>>($"draft/{draft_id}/picks");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromHours(2));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }
}