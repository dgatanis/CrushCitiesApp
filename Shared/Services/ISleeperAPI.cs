using System.Net.Http.Json;
using System.Text.Json;
using System.Net;
using Microsoft.JSInterop;
using Shared.Models;

namespace Shared.Services;

public interface ISleeperAPI
{
    Task<LeagueModel> GetLeagueAsync(string leagueId);
    Task<Dictionary<string, PlayerLiteModel>?> GetNFLPlayerDataAsync();
    Task<NFLStateModel?> GetNFLState();
    Task<List<LeagueModel>> GetLeagueBySeason(string season);
    Task<List<RostersModel>> GetRostersForLeagueAsync(string leagueId);
    Task<List<UsersModel>> GetUsersForLeagueAsync(string leagueId);
    Task<List<DraftsModel>> GetDraftsForLeague(string league_id);
    Task<List<DraftPicksModel>> GetDraftPicksForDraft(string draft_id);
    Task<List<MatchupModel>> GetMatchupsForWeek(string league_id, string week);
}

public sealed class SleeperAPI(HttpClient http) : ISleeperAPI
{
    private readonly HttpClient _http = http;


    /// <summary>
    /// Gets the league details for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public async Task<LeagueModel> GetLeagueAsync(string leagueId)
    {
        if (string.IsNullOrWhiteSpace(leagueId)) return new LeagueModel();
        return await _http.GetFromJsonAsync<LeagueModel>($"league/{leagueId}") ?? new LeagueModel();;

    }
        

    /// <summary>
    /// Gets the rosters for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public async Task<List<RostersModel>> GetRostersForLeagueAsync(string leagueId)
    {
        if (string.IsNullOrWhiteSpace(leagueId)) return new List<RostersModel>();
        return await _http.GetFromJsonAsync<List<RostersModel>>($"league/{leagueId}/rosters") ?? [];
    }
         


    /// <summary>
    /// Gets the users for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public async Task<List<UsersModel>> GetUsersForLeagueAsync(string leagueId)
    {
        if (string.IsNullOrWhiteSpace(leagueId)) return new List<UsersModel>();
        return await _http.GetFromJsonAsync<List<UsersModel>>($"league/{leagueId}/users") ?? [];
    }
        


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
    public async Task<List<LeagueModel>> GetLeagueBySeason(string season)
    {
        if (string.IsNullOrWhiteSpace(season)) return new List<LeagueModel>();
        return await _http.GetFromJsonAsync<List<LeagueModel>>($"user/467550885086490624/leagues/nfl/{season}") ?? [];
    }
        

    /// <summary>
    /// Gets the draft details for a given league
    /// </summary>
    /// <param name="league_id"></param>
    /// <returns></returns>
    public async Task<List<DraftsModel>> GetDraftsForLeague(string league_id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(league_id)) return new List<DraftsModel>();
            return await _http.GetFromJsonAsync<List<DraftsModel>>($"league/{league_id}/drafts") ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    /// <summary>
    /// Gets the draft picks for a given draft 
    /// </summary>
    /// <param name="draft_id"></param>
    /// <returns></returns>
    public async Task<List<DraftPicksModel>> GetDraftPicksForDraft(string draft_id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(draft_id)) return new List<DraftPicksModel>();
            return await _http.GetFromJsonAsync<List<DraftPicksModel>>($"draft/{draft_id}/picks") ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    /// <summary>
    /// Get Matchups for the given week
    /// </summary>
    /// <param name="league_id"></param><param name="week"></param>
    /// <returns></returns>
    public async Task<List<MatchupModel>> GetMatchupsForWeek(string league_id, string week)
    {
        if (string.IsNullOrWhiteSpace(league_id) || string.IsNullOrWhiteSpace(week)) return [];
        return await _http.GetFromJsonAsync<List<MatchupModel>>($"league/{league_id}/matchups/{week}") ?? [];
    }
        
}
