using System.Text.Json;
using Shared.Models;

namespace Shared.Services;

public interface ISleeperAPI
{
    Task<LeagueModel> GetLeagueAsync(string leagueId);
    Task<Dictionary<string, PlayerLiteModel>?> GetNFLPlayerDataAsync();
    Task<NFLStateModel?> GetNFLState();
    Task<List<LeagueModel>> GetLeagueBySeasonAsync(string season);
    Task<List<RostersModel>> GetRostersForLeagueAsync(string leagueId);
    Task<List<UsersModel>> GetUsersForLeagueAsync(string leagueId);
    Task<List<DraftsModel>> GetDraftsForLeagueAsync(string league_id);
    Task<List<DraftPicksModel>> GetDraftPicksForDraftAsync(string draft_id);
    Task<List<MatchupModel>> GetMatchupsForWeekAsync(string league_id, string week);
}

public sealed class SleeperAPI(HttpClient http) : ISleeperAPI
{
    private readonly HttpClient _http = http;
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Ensure the http response returns a 2xx code
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private async Task<string?> GetResponseContentAsync(string path)
    {
        var response = await _http.GetAsync(path);
        response.EnsureSuccessStatusCode();

        if (response.Content is null)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    /// <summary>
    /// Gets an HTTP response and deserializes if there is content in the response
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    private async Task<T?> GetAndDeserializeAsync<T>(string path)
    {
        var content = await GetResponseContentAsync(path);
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }


    /// <summary>
    /// Gets the league details for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public async Task<LeagueModel> GetLeagueAsync(string leagueId)
    {
        if (string.IsNullOrWhiteSpace(leagueId)) return new LeagueModel();
        return await GetAndDeserializeAsync<LeagueModel>($"league/{leagueId}") ?? new LeagueModel();
    }
        

    /// <summary>
    /// Gets the rosters for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public async Task<List<RostersModel>> GetRostersForLeagueAsync(string leagueId)
    {
        if (string.IsNullOrWhiteSpace(leagueId)) return [];
        return await GetAndDeserializeAsync<List<RostersModel>>($"league/{leagueId}/rosters") ?? [];
    }
         


    /// <summary>
    /// Gets the users for a given league from the Sleeper API
    /// </summary>
    /// <param name="leagueId"></param>
    /// <returns></returns>
    public async Task<List<UsersModel>> GetUsersForLeagueAsync(string leagueId)
    {
        if (string.IsNullOrWhiteSpace(leagueId)) return [];
        return await GetAndDeserializeAsync<List<UsersModel>>($"league/{leagueId}/users") ?? [];
    }
        


    /// <summary>
    /// Gets the full NFL player data from the Sleeper API.
    /// </summary>
    /// <returns></returns>
    public async Task<Dictionary<string, PlayerLiteModel>?> GetNFLPlayerDataAsync()
    {
        return await GetAndDeserializeAsync<Dictionary<string, PlayerLiteModel>>("players/nfl");
    }


    /// <summary>
    /// Gets the state of the nfl season
    /// </summary>
    /// <returns></returns>
    public Task<NFLStateModel?> GetNFLState() =>
        GetAndDeserializeAsync<NFLStateModel>("state/nfl");


    /// <summary>
    /// Get the league by season for my user id.
    /// </summary>
    /// <param name="season"></param>
    /// <returns></returns>
    public async Task<List<LeagueModel>> GetLeagueBySeasonAsync(string season)
    {
        if (string.IsNullOrWhiteSpace(season)) return [];
        return await GetAndDeserializeAsync<List<LeagueModel>>($"user/467550885086490624/leagues/nfl/{season}") ?? [];
    }
        

    /// <summary>
    /// Gets the draft details for a given league
    /// </summary>
    /// <param name="league_id"></param>
    /// <returns></returns>
    public async Task<List<DraftsModel>> GetDraftsForLeagueAsync(string league_id)
    {
        if (string.IsNullOrWhiteSpace(league_id)) return [];
        return await GetAndDeserializeAsync<List<DraftsModel>>($"league/{league_id}/drafts") ?? [];
    }

    /// <summary>
    /// Gets the draft picks for a given draft 
    /// </summary>
    /// <param name="draft_id"></param>
    /// <returns></returns>
    public async Task<List<DraftPicksModel>> GetDraftPicksForDraftAsync(string draft_id)
    {
        if (string.IsNullOrWhiteSpace(draft_id)) return [];
        return await GetAndDeserializeAsync<List<DraftPicksModel>>($"draft/{draft_id}/picks") ?? [];
    }

    /// <summary>
    /// Get Matchups for the given week
    /// </summary>
    /// <param name="league_id"></param><param name="week"></param>
    /// <returns></returns>
    public async Task<List<MatchupModel>> GetMatchupsForWeekAsync(string league_id, string week)
    {
        if (string.IsNullOrWhiteSpace(league_id) || string.IsNullOrWhiteSpace(week)) return [];
        var response = await GetAndDeserializeAsync<List<MatchupModel>>($"league/{league_id}/matchups/{week}");

        if (response is null || response.Count == 0) return [];
        return response.Where(m => m.MatchupId is not null).ToList();
    }
        
}
