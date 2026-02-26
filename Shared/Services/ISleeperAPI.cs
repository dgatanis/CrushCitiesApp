using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Shared.Models;

namespace Shared.Services;

public interface ISleeperAPI
{
    Task<LeagueModel?> GetLeagueAsync(string leagueId);
    Task<Dictionary<string, PlayerLiteModel>?> GetNFLPlayerDataAsync();
    Task<NFLStateModel?> GetNFLState();
    Task<List<LeagueModel?>?> GetLeagueBySeason(string season);
    Task<List<RostersModel>?> GetRostersForLeagueAsync(string leagueId);
    Task<List<UsersModel>?> GetUsersForLeagueAsync(string leagueId);
}

public sealed class SleeperAPI(HttpClient http) : ISleeperAPI
{
    private readonly HttpClient _http = http;


    /// <summary>
    /// Gets the league details for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public Task<LeagueModel?> GetLeagueAsync(string leagueId) =>
        _http.GetFromJsonAsync<LeagueModel>($"league/{leagueId}");


    /// <summary>
    /// Gets the rosters for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public Task<List<RostersModel>?> GetRostersForLeagueAsync(string leagueId) =>
         _http.GetFromJsonAsync<List<RostersModel>>($"league/{leagueId}/rosters");


    /// <summary>
    /// Gets the users for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public Task<List<UsersModel>?> GetUsersForLeagueAsync(string leagueId) =>
        _http.GetFromJsonAsync<List<UsersModel>>($"league/{leagueId}/users");


    /// <summary>
    /// Gets the full NFL player data from the Sleeper API.
    /// </summary>
    /// <returns></returns>
    public async Task<Dictionary<string, PlayerLiteModel>?> GetNFLPlayerDataAsync()
    {
        return await _http.GetFromJsonAsync<Dictionary<string, PlayerLiteModel>>("players/nfl");
    }


    /// <summary>
    /// Gets the state of the nfl season
    /// </summary>
    /// <returns></returns>
    public Task<NFLStateModel?> GetNFLState() =>
        _http.GetFromJsonAsync<NFLStateModel>($"state/nfl");


    /// <summary>
    /// Get the league by season for my user id.
    /// </summary>
    /// <param name="season"></param>
    /// <returns></returns>
    public Task<List<LeagueModel?>?> GetLeagueBySeason(string season) =>
        _http.GetFromJsonAsync<List<LeagueModel?>?>($"user/467550885086490624/leagues/nfl/{season}");
 
}
