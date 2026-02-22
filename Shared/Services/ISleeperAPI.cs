using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Shared.Models;

namespace Shared.Services;

public interface ISleeperAPI
{
    Task<LeagueModel?> GetLeagueAsync(string leagueId);
    Task<Dictionary<string, PlayerLiteModel>?> GetNFLPlayerDataAsync(bool forceRefresh = false);
    Task<PlayerLiteModel?> GetNFLPlayerByIdAsync(string playerId, bool forceRefresh = false);
    Task ClearNFLPlayersCacheAsync();
    Task<List<RostersModel>?> GetRostersForLeagueAsync(string leagueId);
    Task<UsersModel?> GetUsersForLeagueAsync(string leagueId);
}

public sealed class SleeperAPI : ISleeperAPI
{
    private const string NflPlayersCacheKey = "sleeper:nfl:players-lite:v1";
    private static readonly TimeSpan NflPlayersCacheTtl = TimeSpan.FromHours(6);

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private Dictionary<string, PlayerLiteModel>? _memoryPlayers;
    private DateTimeOffset _memoryFetchedAtUtc;

    public SleeperAPI(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }


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
    public Task<UsersModel?> GetUsersForLeagueAsync(string leagueId) =>
        _http.GetFromJsonAsync<UsersModel>($"league/{leagueId}/users");


    /// <summary>
    /// Gets the full NFL player data from the Sleeper API.
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task<Dictionary<string, PlayerLiteModel>?> GetNFLPlayerDataAsync(bool forceRefresh = false)
    {
        if (!forceRefresh &&
            _memoryPlayers is not null &&
            _memoryFetchedAtUtc > DateTimeOffset.UtcNow - NflPlayersCacheTtl)
        {
            return _memoryPlayers;
        }

        if (!forceRefresh)
        {
            var cachedJson = await _js.InvokeAsync<string?>("localStorage.getItem", NflPlayersCacheKey);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cached = JsonSerializer.Deserialize<PlayersCacheModel>(cachedJson);
                if (cached is not null &&
                    cached.Data is not null &&
                    cached.FetchedAtUtc > DateTimeOffset.UtcNow - NflPlayersCacheTtl)
                {
                    _memoryPlayers = cached.Data;
                    _memoryFetchedAtUtc = cached.FetchedAtUtc;
                    return cached.Data;
                }
            }
        }

        var fullData = await _http.GetFromJsonAsync<Dictionary<string, PlayersModel>>("players/nfl");
        if (fullData is null)
        {
            return null;
        }

        var liteData = fullData.ToDictionary(
            kvp => kvp.Key,
            kvp => new PlayerLiteModel
            {
                PlayerId = kvp.Value.PlayerId,
                Position = kvp.Value.Position,
                Firstname = kvp.Value.FirstName,
                Lastname = kvp.Value.LastName,
                Age = 0,
                Team = kvp.Value.PlayerId,
                Number = null,
                Height = null,
                Weight = null,
                YearsExp = null,
                RotowireId = null,
                College = null,
                SearchRank = null,
                InjuryStatus = null,
                InjuryBodyPart = null
            });

        var payload = new PlayersCacheModel
        {
            FetchedAtUtc = DateTimeOffset.UtcNow,
            Data = liteData
        };

        _memoryPlayers = liteData;
        _memoryFetchedAtUtc = payload.FetchedAtUtc;

        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", NflPlayersCacheKey, JsonSerializer.Serialize(payload));
        }
        catch (JSException)
        {
            // localStorage quota can be exceeded for large payloads.
        }

        return liteData;
    }

    /// <summary>
    /// Gets the full NFL player data from the Sleeper API, then returns the specific player matching the provided ID. 
    /// This method is optimized to utilize cached data when available to minimize API calls and improve performance.
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task<PlayerLiteModel?> GetNFLPlayerByIdAsync(string playerId, bool forceRefresh = false)
    {
        var players = await GetNFLPlayerDataAsync(forceRefresh);
        if (players is null)
        {
            return null;
        }

        return players.TryGetValue(playerId, out var player) ? player : null;
    }


    /// <summary>
    /// Clears the cached NFL player data from both memory and localStorage. This can be used to force a refresh on the next data retrieval.
    /// </summary>
    /// <returns></returns>
    public Task ClearNFLPlayersCacheAsync() =>
        ClearCacheInternalAsync();

    /// <summary>
    /// Clears the cached NFL player data from both memory and localStorage. This can be used to force a refresh on the next data retrieval.
    /// </summary>
    /// <returns></returns>
    private async Task ClearCacheInternalAsync()
    {
        _memoryPlayers = null;
        _memoryFetchedAtUtc = default;
        await _js.InvokeVoidAsync("localStorage.removeItem", NflPlayersCacheKey);
    }

    /// <summary>
    /// Internal model used for caching the NFL player data in localStorage, including a timestamp for cache invalidation logic.
    /// </summary>
    private sealed class PlayersCacheModel
    {
        public DateTimeOffset FetchedAtUtc { get; set; }
        public Dictionary<string, PlayerLiteModel>? Data { get; set; }
    }
}
