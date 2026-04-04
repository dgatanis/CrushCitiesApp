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
    private Task? _cacheTask;
    
    private Task? _loadTask;
    private bool _dataLoaded = false;
    private bool _cacheLoaded = false;

    /// <summary>
    /// Lookup dictionary for getting a PlayerLite model by providing the player_id. 
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public readonly Dictionary<string, PlayerLiteModel> PlayerById = new();
    
    /// <summary>
    /// Lookup dictionary for getting a players nfl team image by providing the team_abbr (MIA, TEN, WAS...).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<string, string> PlayerNFLTeamImageByAbbr = new();

    /// <summary>
    /// Dictionary to store the player_id by the PlayerLiteModel. 
    /// Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<string, PlayerLiteModel>? Players { get; private set; }

    /// <summary>
    /// Ensures that the Players data is loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;

    /// <summary>
    /// Ensures the lookups are loaded
    /// </summary>
    private bool IsCacheLoaded => _cacheLoaded;


    /// <summary>
    /// Sets the players in local storage and sets an expiration for that data
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task SetPlayersAsync(bool forceRefresh = false)
    {
        try
        {
            if (!forceRefresh &&
            _memoryPlayers is not null &&
            _memoryExpiration > DateTimeOffset.UtcNow - NflPlayersCacheTtl)
            {
                Players = _memoryPlayers;
                _dataLoaded = true;

                return;
            }

            if (!IsLoaded || forceRefresh) // Only force refresh if the data isn't there or its older than the TTL
            {
                var cachedJson = await _js.InvokeAsync<string?>("localStorage.getItem", NflPlayersCacheKey);
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cached = default(PlayersCacheModel);
                    try
                    {
                        cached = JsonSerializer.Deserialize<PlayersCacheModel>(cachedJson);
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Failed to deserialize cached player data: {ex.Message}");
                    }
                    
                    if (cached is not null &&
                        cached.Data is not null &&
                        cached.Expiration > DateTimeOffset.UtcNow)
                    {
                        _memoryPlayers = cached.Data;
                        _memoryExpiration = cached.Expiration;
                        Players = cached.Data;
                        _dataLoaded = true;
                        await BuildLookupsAsync();
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
            }
            
            _dataLoaded = true;
            await BuildLookupsAsync();
        }
        catch (Exception ex)
        {
            _dataLoaded = false;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }

    }


    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    private async Task BuildLookupsAsync()
    {
        try
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
                _cacheLoaded = true;
                return;
            }
            
            _cacheLoaded = false;
        }
        catch (Exception ex)
        {
            _cacheLoaded = false;
            _cacheTask = null;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }

    }

    /// <summary>
    /// Ensures Players data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetPlayersAsync(forceRefresh: true);
        return _loadTask;
    }

    /// <summary>
    /// Ensures the lookup data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLookupsLoadedAsync()
    {
        if (IsCacheLoaded) return Task.CompletedTask;
        _cacheTask ??= BuildLookupsAsync();
        return _cacheTask;
    }


    /// <summary>
    /// Internal model used for caching the NFL player data in localStorage, including a timestamp for cache invalidation logic.
    /// </summary>
    private sealed class PlayersCacheModel
    {
        public DateTimeOffset Expiration { get; set; }
        public Dictionary<string, PlayerLiteModel>? Data { get; set; }
    }
}