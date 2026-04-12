using Shared.Models;
using System.Net.Http.Json;


namespace SleeperFunctions.Application;

public sealed class MatchupService(IHttpClientFactory http, ICacheService cache) : IMatchupService
{
    private readonly IHttpClientFactory _httpFactory = http;
    private readonly ICacheService _cache = cache;


    public async Task<List<MatchupModel>> GetMatchupsAsync(string league_id, string week)
    {
        var cacheKey = $"sleeper-matchups-{league_id}-{week}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<MatchupModel>>($"league/{league_id}/matchups/{week}")
                                                        ?? new List<MatchupModel>(),
                                          TimeSpan.FromMinutes(10));
    }

    public async Task<List<PlayoffBracketsModel>> GetPlayoffWinnersAsync(string league_id)
    {
        var cacheKey = $"sleeper-winners-bracket-{league_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<PlayoffBracketsModel>>($"league/{league_id}/winners_bracket")
                                                    ?? new List<PlayoffBracketsModel>(),
                                          TimeSpan.FromHours(6)); 
    }

    public async Task<List<PlayoffBracketsModel>> GetPlayoffLosersAsync(string league_id)
    {
        var cacheKey = $"sleeper-losers-bracket-{league_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<PlayoffBracketsModel>>($"league/{league_id}/losers_bracket")
                                                    ?? new List<PlayoffBracketsModel>(),
                                          TimeSpan.FromHours(6));

    }
}