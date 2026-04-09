using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Shared.Models;

namespace Shared.Services;


/// <summary>
/// Service that stores the transactions for the current league and allows filtering by type (["trade", "waiver", or "free_agent"]).
/// </summary>
/// <param name="sleeperApi"></param>
/// <param name="leagueData"></param>
/// <param name="http"></param>
/// <param name="logger"></param>
public sealed class TransactionData(ISleeperAPI sleeperApi, LeagueData leagueData, HttpClient http, ILogger<TransactionData> logger)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueData _leagueData = leagueData;
    private readonly HttpClient _http = http;
    private readonly ILogger<TransactionData> _logger = logger;
    private Task? _loadTask;
    private bool _dataLoaded = false;
    
    private List<TransactionsModel> _transactions = new List<TransactionsModel>();

    /// <summary>
    /// List of all transactions. Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<TransactionsModel> Transactions => _transactions;

    /// <summary>
    /// Ensures Transactions data is loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;


    /// <summary>
    /// Sets all transaction data beginning from the currentleagueid looping backwards
    /// Appends "extra" fleaflicker trade data from a json
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task SetAllTransactionsDataAsync(bool forceRefresh = false)
    {
        try
        {
            var json = await _http.GetStringAsync("/data/fleaflicker_trades_data.json");
            var fleaflicker_trades = JsonSerializer.Deserialize<List<TransactionsModel>>(json);

            if (!IsLoaded || forceRefresh)
            {
                _transactions.Clear();
                await _leagueData.EnsureLoadedAsync();
                
                foreach(var league in _leagueData.AllLeagues)
                {
                    if (league.Settings?.LastScoredLeg is not null && league.LeagueId is not null)
                    {
                        var lastWeek = Math.Max(1, league.Settings.LastScoredLeg); //If 0 use week 1 instead
                        for(int i = lastWeek; i > 0; i--)
                        {
                            var transactions = await _sleeperApi.GetTransactionsForWeekAsync(league.LeagueId, i.ToString());

                            if (transactions is not null)
                            {
                                _transactions.AddRange(transactions);
                            }
                        }
                    }
                }

                if (fleaflicker_trades is not null)
                {
                    _transactions.AddRange(fleaflicker_trades);
                }
            }
            _dataLoaded = true;
        }
        catch (Exception ex)
        {
            _loadTask = null;
            _dataLoaded = false;
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
            throw;
        }
    }


    /// <summary>
    /// Returns the filtered transaction data based on the types
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public IReadOnlyList<TransactionsModel> GetFilteredTransactionsData(string[] types)
    {
        if (Transactions is null || types is null || types.Length == 0)
            return Transactions ?? [];

        return Transactions
            .Where(t => types.Contains(t.Type))
            .OrderByDescending(t => t.Created)
            .ToList();
    }

    /// <summary>
    /// Ensures Transactions data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllTransactionsDataAsync(forceRefresh: true);
        return _loadTask;
    }
}
