using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions.Application;

public sealed class UserService(ICacheService cache, IHttpClientFactory http) : IUserService
{
    private readonly ICacheService _cache = cache;
    private readonly IHttpClientFactory _httpFactory = http;


    /// <summary>
    /// Gets the users for a given league_id.
    /// </summary>
    /// <param name="league_id"></param>
    /// <returns>List<UsersModel></returns>
    public async Task<List<UsersModel>> GetUsersAsync(string league_id)
    {
        var cacheKey = $"sleeper-users-{league_id}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<UsersModel>>($"league/{league_id}/users")
                                                    ?? new List<UsersModel>(),
                                          TimeSpan.FromMinutes(10));
    }
}