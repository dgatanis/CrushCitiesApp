using System.Text.Json;
using Microsoft.JSInterop;
using Shared.Models;

namespace Shared.Services;

public sealed class PlayerState(ISleeperAPI sleeperApi, IJSRuntime js)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly IJSRuntime _js = js;
    private const string NflPlayersCacheKey = "sleeper:nfl:players-lite:v1";
    private static readonly TimeSpan NflPlayersCacheTtl = TimeSpan.FromHours(6);
    private Dictionary<string, PlayerLiteModel>? _memoryPlayers;
    private DateTimeOffset _memoryExpiration;

    
    public readonly Dictionary<string, PlayerLiteModel> PlayerById = new();
    public Dictionary<string, string> PlayerNFLTeamImageByAbbr = new();
    public Dictionary<string, PlayerLiteModel>? Players { get; private set; }
    public bool IsLoaded => Players is not null;
    public bool CacheLoaded => PlayerNFLTeamImageByAbbr.Count > 0 && PlayerById.Count > 0;


    /// <summary>
    /// Sets the players in local storage and sets an expiration for that data
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetPlayersAsync(bool forceRefresh = false)
    {
        if (!forceRefresh &&
            _memoryPlayers is not null &&
            _memoryExpiration > DateTimeOffset.UtcNow - NflPlayersCacheTtl)
        {
            Players = _memoryPlayers;
            return;
        }

        if (!IsLoaded || forceRefresh) // Only force refresh if the data isn't there or its older than the TTL
        {
            var cachedJson = await _js.InvokeAsync<string?>("localStorage.getItem", NflPlayersCacheKey);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cached = JsonSerializer.Deserialize<PlayersCacheModel>(cachedJson);
                if (cached is not null &&
                    cached.Data is not null &&
                    cached.Expiration > DateTimeOffset.UtcNow)
                {
                    _memoryPlayers = cached.Data;
                    _memoryExpiration = cached.Expiration;
                    Players = cached.Data;
                    await BuildLookupCachesAsync();
                    return;
                }
                else
                {
                    var fullData = await _sleeperApi.GetNFLPlayerDataAsync();
                    if (fullData is not null)
                    {
                        var liteData = fullData.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new PlayerLiteModel
                        {
                            PlayerId = kvp.Value.PlayerId,
                            Position = kvp.Value.Position,
                            Firstname = kvp.Value.Firstname,
                            Lastname = kvp.Value.Lastname,
                            Age = kvp.Value.Age,
                            Team = kvp.Value.Team,
                            Number = kvp.Value.Number,
                            Height = kvp.Value.Height,
                            Weight = kvp.Value.Weight,
                            YearsExp = kvp.Value.YearsExp,
                            RotowireId = kvp.Value.RotowireId,
                            College = kvp.Value.College,
                            SearchRank = kvp.Value.SearchRank,
                            InjuryStatus = kvp.Value.InjuryStatus,
                            InjuryBodyPart = kvp.Value.InjuryBodyPart
                        });

                        _memoryPlayers = liteData;
                        _memoryExpiration = DateTimeOffset.UtcNow + NflPlayersCacheTtl;
                        Players = liteData;

                        var payload = new PlayersCacheModel
                        {
                            Expiration = _memoryExpiration,
                            Data = liteData
                        };

                        await _js.InvokeVoidAsync("localStorage.setItem", NflPlayersCacheKey, JsonSerializer.Serialize(payload));
                    }
                }
            }
            else
            {
                var fullData = await _sleeperApi.GetNFLPlayerDataAsync();
                if (fullData is not null)
                {
                    var liteData = fullData.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new PlayerLiteModel
                    {
                        PlayerId = kvp.Value.PlayerId,
                        Position = kvp.Value.Position,
                        Firstname = kvp.Value.Firstname,
                        Lastname = kvp.Value.Lastname,
                        Age = kvp.Value.Age,
                        Team = kvp.Value.Team,
                        Number = kvp.Value.Number,
                        Height = kvp.Value.Height,
                        Weight = kvp.Value.Weight,
                        YearsExp = kvp.Value.YearsExp,
                        RotowireId = kvp.Value.RotowireId,
                        College = kvp.Value.College,
                        SearchRank = kvp.Value.SearchRank,
                        InjuryStatus = kvp.Value.InjuryStatus,
                        InjuryBodyPart = kvp.Value.InjuryBodyPart
                    });

                    _memoryPlayers = liteData;
                    _memoryExpiration = DateTimeOffset.UtcNow + NflPlayersCacheTtl;
                    Players = liteData;

                    var payload = new PlayersCacheModel
                    {
                        Expiration = _memoryExpiration,
                        Data = liteData
                    };

                    await _js.InvokeVoidAsync("localStorage.setItem", NflPlayersCacheKey, JsonSerializer.Serialize(payload));
                }
            }
            await BuildLookupCachesAsync();
        }
    }


    /// <summary>
    /// Internal model used for caching the NFL player data in localStorage, including a timestamp for cache invalidation logic.
    /// </summary>
    private sealed class PlayersCacheModel
    {
        public DateTimeOffset Expiration { get; set; }
        public Dictionary<string, PlayerLiteModel>? Data { get; set; }
    }


    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    public async Task BuildLookupCachesAsync()
    {
        if(_memoryPlayers is not null)
        {
            PlayerById.Clear();
            PlayerNFLTeamImageByAbbr.Clear();

            //Get Player by id
            foreach (var player in _memoryPlayers)
            {
                PlayerById[player.Key] = player.Value;
            }

            //Get nfl team image by abbreviation
            foreach (var teamAbbr in _memoryPlayers.Values.Select(p => p.Team).Distinct()) 
            {
                if (string.IsNullOrWhiteSpace(teamAbbr)) continue;

                if (teamAbbr != "FA")
                {
                    PlayerNFLTeamImageByAbbr[teamAbbr] =
                        $"https://sleepercdn.com/images/team_logos/nfl/{teamAbbr.ToLower()}.png";
                }
                else
                {
                    PlayerNFLTeamImageByAbbr[teamAbbr] = "/images/question-mark.png";
                }
            }
        }
        else
        {
            await SetPlayersAsync();
        }
    }
}
