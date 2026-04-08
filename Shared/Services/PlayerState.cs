using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shared.Models;

namespace Shared.Services;


/// <summary>
/// Service that stores the NFL player data for the current league and builds lookups for frequently accessed data.
/// Utilizes localStorage to cache the player data and reduce load times on subsequent visits, with a TTL of 6 hours for cache invalidation.
/// </summary>
/// <param name="sleeperApi"></param>
/// <param name="js"></param>
/// <param name="logger"></param>
public sealed class PlayerState(ISleeperAPI sleeperApi, IJSRuntime js, ILogger<PlayerState> logger)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly IJSRuntime _js = js;
    private readonly ILogger<PlayerState> _logger = logger;
    private const string NflPlayersCacheKey = "sleeper:nfl:players-lite:v1";
    private static readonly TimeSpan NflPlayersCacheTtl = TimeSpan.FromHours(6);
    private Dictionary<string, PlayerLiteModel>? _memoryPlayers;
    private DateTimeOffset _memoryExpiration;
    private Task? _lookupTask;
    private Task? _loadTask;
    private bool _dataLoaded = false;
    private bool _lookupsLoaded = false;
    
    private Dictionary<string, PlayerLiteModel> _playerById = new();
    private Dictionary<string, string> _playerNFLTeamImageByAbbr = new();

    /// <summary>
    /// Lookup dictionary for getting a PlayerLite model by providing the player_id. 
    /// Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<string, PlayerLiteModel> PlayerById => _playerById;
    
    /// <summary>
    /// Lookup dictionary for getting a players nfl team image by providing the team_abbr (MIA, TEN, WAS...).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<string, string> PlayerNFLTeamImageByAbbr => _playerNFLTeamImageByAbbr;

    /// <summary>
    /// Ensures that the Players data is loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;

    /// <summary>
    /// Ensures the lookups are loaded
    /// </summary>
    private bool IsLookupLoaded => _lookupsLoaded;


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
            _memoryPlayers.Count > 0 &&
            _memoryExpiration > DateTimeOffset.UtcNow)
            {
                _playerById = _memoryPlayers;
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
                        _logger.LogWarning(ex, "Failed to deserialize cached player data");
                    }
                    
                    if (cached is not null &&
                        cached.Data is not null &&
                        cached.Expiration > DateTimeOffset.UtcNow)
                    {
                        _memoryPlayers = cached.Data;
                        _memoryExpiration = cached.Expiration;
                        _playerById = cached.Data;
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
                            _playerById = liteData;

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
                        _playerById = liteData;

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
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
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
                _playerNFLTeamImageByAbbr.Clear();

                //Get nfl team image by abbreviation
                foreach (var teamAbbr in _memoryPlayers.Values.Select(p => p.Team).Distinct()) 
                {
                    if (string.IsNullOrWhiteSpace(teamAbbr)) continue;

                    if (teamAbbr != "FA")
                    {
                        _playerNFLTeamImageByAbbr[teamAbbr] =
                            $"https://sleepercdn.com/images/team_logos/nfl/{teamAbbr.ToLower()}.png";
                    }
                    else
                    {
                        _playerNFLTeamImageByAbbr[teamAbbr] = "/images/question-mark.png";
                    }
                }
                _lookupsLoaded = true;
                return;
            }
            
            _lookupsLoaded = false;
        }
        catch (Exception ex)
        {
            _lookupsLoaded = false;
            _lookupTask = null;
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
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
        if (IsLookupLoaded) return Task.CompletedTask;
        _lookupTask ??= BuildLookupsAsync();
        return _lookupTask;
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

