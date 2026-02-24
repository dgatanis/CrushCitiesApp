using System.Text.Json;
using Microsoft.JSInterop;
using Shared.Models;

namespace Shared.Services;

public sealed class PlayerState(ISleeperAPI sleeperApi, IJSRuntime js)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly IJSRuntime _js = js;
    private const string NflPlayersCacheKey = "sleeper:nfl:players-lite:v1";
    public Dictionary<string, PlayerLiteModel>? Players { get; private set; }
    public bool IsLoaded => Players is not null;
    private static readonly TimeSpan NflPlayersCacheTtl = TimeSpan.FromHours(6);
    private Dictionary<string, PlayerLiteModel>? _memoryPlayers;
    private DateTimeOffset _memoryExpiration;


    public async Task SetPlayers(bool forceRefresh = false)
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
    }

    public PlayerLiteModel? GetByPlayerId(string playerId)
    {
        if (_memoryPlayers is not null &&
            _memoryExpiration > DateTimeOffset.UtcNow &&
            _memoryPlayers.TryGetValue(playerId, out var memoryPlayer))
        {
            return memoryPlayer;
        }

        if (_js is IJSInProcessRuntime inProcess)
        {
            var cachedJson = inProcess.Invoke<string?>("localStorage.getItem", NflPlayersCacheKey);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                var cached = JsonSerializer.Deserialize<PlayersCacheModel>(cachedJson);
                if (cached is not null &&
                    cached.Data is not null &&
                    cached.Expiration > DateTimeOffset.UtcNow - NflPlayersCacheTtl)
                {
                    _memoryPlayers = cached.Data;
                    _memoryExpiration = cached.Expiration;
                    Players = cached.Data;
                    return cached.Data.TryGetValue(playerId, out var player) ? player : null;
                }
            }
        }

        return null;
    }

    public PlayerLiteModel? GetById(string playerId) =>
        GetByPlayerId(playerId);

    /// <summary>
    /// Internal model used for caching the NFL player data in localStorage, including a timestamp for cache invalidation logic.
    /// </summary>
    private sealed class PlayersCacheModel
    {
        public DateTimeOffset Expiration { get; set; }
        public Dictionary<string, PlayerLiteModel>? Data { get; set; }
    }
}
