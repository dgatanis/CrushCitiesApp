using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions.Application;

public sealed class DraftService(IHttpClientFactory http, ICacheService cache) : IDraftService
{
    private readonly IHttpClientFactory _httpFactory = http;
    private readonly ICacheService _cache = cache;

    public async Task<List<DraftsModel>> GetDraftsAsync(string league_id)
    {
        var cacheKey = $"sleeper-drafts-{league_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey, 
                                          async () => await client.GetFromJsonAsync<List<DraftsModel>>($"league/{league_id}/drafts")
                                                    ?? new List<DraftsModel>(),
                                          TimeSpan.FromHours(2));

    }


    public async Task<List<DraftPicksModel>> GetDraftPicksAsync(string draft_id)
    {
        var cacheKey = $"sleeper-draft-picks-{draft_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<DraftPicksModel>>($"draft/{draft_id}/picks")
                                                    ?? new List<DraftPicksModel>(),
                                          TimeSpan.FromHours(2));

    }
}