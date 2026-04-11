using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions;

public class TransactionFunctions(ILogger<TransactionFunctions> logger, IHttpClientFactory http, IMemoryCache cache)
{
    private readonly ILogger<TransactionFunctions> _logger = logger;
    private readonly HttpClient _http = http.CreateClient("SleeperClient");
    private readonly IMemoryCache _cache = cache;

    [Function("GetTransactionsAsync")]
    public async Task<IActionResult> GetTransactionsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "league/{league_id}/transactions/{week}")] HttpRequest req, string league_id, string week)
    {
        var cacheKey = $"sleeper-transactions-{league_id}-{week}";

        if (!_cache.TryGetValue(cacheKey, out var cachedData))
        {
            _logger.LogDebug("Cache miss [{CacheKey}] - fetching from Sleeper", cacheKey);
            cachedData = await _http.GetFromJsonAsync<List<TransactionsModel>>($"league/{league_id}/transactions/{week}");
            _cache.Set(cacheKey, cachedData, TimeSpan.FromMinutes(30));
        }
        else
        {
            _logger.LogDebug("Cache hit [{CacheKey}]", cacheKey);
        }

        return new OkObjectResult(cachedData);
    }
}