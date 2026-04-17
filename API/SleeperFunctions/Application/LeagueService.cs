using Microsoft.Extensions.Configuration;
using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions.Application;

public sealed class LeagueService(IHttpClientFactory http, IConfiguration config, ICacheService cache) : ILeagueService
{
    private readonly IHttpClientFactory _httpFactory = http;
    private readonly string _userId = config["SleeperUserId"]!;
    private readonly ICacheService _cache = cache;


    /// <summary>
    /// Gets the league details for a given league_id.
    /// </summary>
    /// <param name="league_id"></param>
    /// <returns>LeagueModel</returns>
    public async Task<LeagueModel> GetLeagueByIdAsync(string league_id)
    {
        var cacheKey = $"sleeper-leagues-{league_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<LeagueModel>($"league/{league_id}")
                                                ?? new LeagueModel(),
                                          TimeSpan.FromHours(2));
    }


    /// <summary>
    /// Returns all leagues for the user for a given season.
    /// </summary>
    /// <param name="season"></param>
    /// <returns>List<LeagueModel></returns>
    public async Task<List<LeagueModel>> GetLeagueBySeasonAsync(string season)
    {
        var cacheKey = $"sleeper-leagues-by-season-{season}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<LeagueModel>>($"user/{_userId}/leagues/nfl/{season}")
                                                ?? new List<LeagueModel>(),
                                          TimeSpan.FromHours(2));

    }


    /// <summary>
    /// Gets the current state of the nfl season from sleeper.
    /// </summary>
    /// <returns>NFLStateModel</returns>
    public async Task<NFLStateModel> GetNFLStateAsync()
    {
        var cacheKey = "sleeper-nfl-state";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<NFLStateModel>($"state/nfl")
                                                   ?? new NFLStateModel(),
                                          TimeSpan.FromMinutes(10));
    }

}