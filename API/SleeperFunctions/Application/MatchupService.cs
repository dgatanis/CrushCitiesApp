using Shared.Models;
using System.Net.Http.Json;


namespace SleeperFunctions.Application;

public sealed class MatchupService(IHttpClientFactory http, ICacheService cache) : IMatchupService
{
    private readonly IHttpClientFactory _httpFactory = http;
    private readonly ICacheService _cache = cache;


    /// <summary>
    /// Gets the matchups for a given league_id and week.
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="week"></param>
    /// <returns>List<MatchupModel></returns>
    public async Task<List<MatchupModel>> GetMatchupsAsync(string league_id, string week)
    {
        var cacheKey = $"sleeper-matchups-{league_id}-{week}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<MatchupModel>>($"league/{league_id}/matchups/{week}")
                                                        ?? new List<MatchupModel>(),
                                          TimeSpan.FromMinutes(10));
    }


    /// <summary>
    /// Gets the playoff winners bracket for a given league_id.
    /// </summary>
    /// <param name="league_id"></param>
    /// <returns>List<PlayoffBracketsModel></returns>
    public async Task<List<PlayoffBracketsModel>> GetPlayoffWinnersAsync(string league_id)
    {
        var cacheKey = $"sleeper-winners-bracket-{league_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<PlayoffBracketsModel>>($"league/{league_id}/winners_bracket")
                                                    ?? new List<PlayoffBracketsModel>(),
                                          TimeSpan.FromHours(6)); 
    }


    /// <summary>
    /// Gets the playoff losers bracket for a given league_id.
    /// </summary>
    /// <param name="league_id"></param>
    /// <returns>List<PlayoffBracketsModel></returns>
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