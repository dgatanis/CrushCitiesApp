using System.Text.Json;
using Microsoft.VisualBasic;
using Shared.Models;

namespace Shared.Services;

public sealed class TransactionState(ISleeperAPI sleeperApi, LeagueState leagueState, HttpClient http)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;

    private readonly HttpClient _http = http;
    
    private Task? _loadTask;

    /// <summary>
    /// List of all transactions. 
    /// Set via SetAllTransactionsDataAsync()
    /// </summary>
    public List<TransactionsModel> Transactions { get; private set; } = new();

    /// <summary>
    /// List of filtered transactions filtered by using FilterTransactionsData(type). 
    /// Can dynamically update list based on "type" needs.
    /// </summary>
    public List<TransactionsModel> FilteredTransactions { get; private set; } = new();

    /// <summary>
    /// Ensures transactions are loaded
    /// </summary>
    public bool IsLoaded => Transactions is not null && Transactions.Count > 0;


    /// <summary>
    /// Sets all transaction data beginning from the currentleagueid looping backwards
    /// Appends "extra" fleaflicker trade data from a json
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllTransactionsDataAsync(bool forceRefresh = false)
    {
        var json = await _http.GetStringAsync("/data/fleaflicker_trades_data.json");
        var fleaflicker_trades = JsonSerializer.Deserialize<List<TransactionsModel>>(json);
        Transactions.Clear();
        if (!IsLoaded || forceRefresh)
        {
            
            await _leagueState.EnsureLoadedAsync();
            
            foreach(var league in _leagueState.AllLeagues)
            {
                if (league.Settings?.LastScoredLeg is not null && league.LeagueId is not null)
                {
                    var lastWeek = Math.Max(1, league.Settings.LastScoredLeg); //If 0 use week 1 instead
                    for(int i = lastWeek; i > 0; i--)
                    {
                        var transactions = await _sleeperApi.GetTransactionsForWeekAsync(league.LeagueId, i.ToString());

                        if (transactions is not null)
                        {
                            Transactions.AddRange(transactions);
                        }
                    }
                }
            }

            if (fleaflicker_trades is not null)
            {
                Transactions.AddRange(fleaflicker_trades);
            }
        }
    }


    /// <summary>
    /// Returns the filtered transaction data based on the types
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public IReadOnlyList<TransactionsModel> GetFilterTransactionsData(string[] types)
    {
        if (Transactions is null || types is null || types.Length == 0)
            return Transactions ?? [];

        return Transactions
            .Where(t => types.Contains(t.Type))
            .OrderByDescending(t => t.Created)
            .ToList();
    }

    /// <summary>
    /// Ensures that the Transactions data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllTransactionsDataAsync(forceRefresh: true);
        return _loadTask;
    }
}
