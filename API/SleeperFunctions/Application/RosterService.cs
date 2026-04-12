using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions.Application;

public sealed class RosterService(IHttpClientFactory http, ICacheService cache) : IRosterService
{
    private readonly IHttpClientFactory _httpFactory = http;
    private readonly ICacheService _cache = cache;

    public async Task<List<RostersModel>> GetRostersAsync(string league_id)
    {
        var cacheKey = $"sleeper-rosters-{league_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<RostersModel>>($"league/{league_id}/rosters")
                                                    ?? new List<RostersModel>(),
                                          TimeSpan.FromHours(2));

    }
}