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
            
            if(!_leagueState.IsLoadedAllLeagues) await _leagueState.SetAllLeaguesDataAsync();
            
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
    /// Filter the transaction data based on the type
    /// Sets the FilteredTransactions variable to the transactions that match the type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public void FilterTransactionsData(string type = "")
    {

        FilteredTransactions.Clear();
        
        if(Transactions is not null)
        {
            
            if(!string.IsNullOrEmpty(type))
            {
                FilteredTransactions.AddRange(Transactions.Where(x => x.Type == type).OrderByDescending(t => t.Created));
            }
            else
            {
                FilteredTransactions = Transactions;
            }
        }
    }


    /// <summary>
    /// Verify filtered transactions contains all types 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public bool FilteredTransactionsContainsTypes(string[] types)
    {
        if(FilteredTransactions.Count == 0) return false;
        var typesCount = types.Length;
        var counter = 0;
        foreach(var type in types)
        {
            var transactions = FilteredTransactions.Where(x => x.Type == type);
            
            if(transactions.Count() > 0)
            {
                counter++;
            }
        }
        if(counter == typesCount) return true;
        return false;
    }

    /// <summary>
    /// Ensures that the transactions data is loaded
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllTransactionsDataAsync();
        return _loadTask;
    }
}
