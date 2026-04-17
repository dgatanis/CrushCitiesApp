using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions.Application;

public sealed class PlayerService(ICacheService cache, IHttpClientFactory http) : IPlayerService
{
    private readonly ICacheService _cache = cache;
    private readonly IHttpClientFactory _httpFactory = http;


    /// <summary>
    /// Gets the NFL players from Sleeper. Cached for 6 hours since the payload is large and data doesn't change often.
    /// </summary>
    /// <returns>Dictionary<string, PlayersModel></returns>
    public async Task<Dictionary<string, PlayersModel>> GetPlayersAsync()
    {
        var cacheKey = "sleeper-players";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<Dictionary<string, PlayersModel>>("players/nfl")
                                                    ?? new Dictionary<string, PlayersModel>(),
                                          TimeSpan.FromHours(6));
    }
}