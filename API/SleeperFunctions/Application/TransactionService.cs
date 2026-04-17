using Shared.Models;
using System.Net.Http.Json;

namespace SleeperFunctions.Application;

public sealed class TransactionService(ICacheService cache, IHttpClientFactory http) : ITransactionService
{
    private readonly IHttpClientFactory _httpFactory = http;
    private readonly ICacheService _cache = cache;


    /// <summary>
    /// Gets the transactions for a given league_id and week.
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="week"></param>
    /// <returns>TransactionsModel</returns>
    public async Task<List<TransactionsModel>> GetTransactionsAsync(string league_id, string week)
    {
        var cacheKey = $"sleeper-transactions-{league_id}-{week}";
        var client = _httpFactory.CreateClient("SleeperClient");

        return await _cache.GetOrSetAsync(cacheKey,
                                          async () => await client.GetFromJsonAsync<List<TransactionsModel>>($"league/{league_id}/transactions/{week}")
                                                    ?? new List<TransactionsModel>(),
                                          TimeSpan.FromMinutes(10));
    }
}