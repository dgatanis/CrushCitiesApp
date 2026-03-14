using System.Text.Json;
using Shared.Models;

namespace Shared.Services;

public sealed class TransactionState(ISleeperAPI sleeperApi, LeagueState leagueState, HttpClient http)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;

    private readonly HttpClient _http = http;
    
    public List<TransactionsModel> Transactions { get; private set; } = new();
    public List<TransactionsModel> FilteredTransactions { get; private set; } = new();
    public bool IsLoaded => Transactions is not null && Transactions.Count > 0;


    /// <summary>
    /// Sets all transaction data beginning from the currentleagueid looping backwards
    /// Appends "extra" fleaflicker trade data from a json
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllTransactionsData(bool forceRefresh = false)
    {
        var json = await _http.GetStringAsync("/data/fleaflicker_trades_data.json");
        var fleaflicker_trades = JsonSerializer.Deserialize<List<TransactionsModel>>(json);
        
        if (!IsLoaded || forceRefresh)
        {
            var leagueId = await _leagueState.GetCurrentLeagueId();
            
            while (!string.IsNullOrWhiteSpace(leagueId))
            {
                await _leagueState.SetLeague(leagueId, forceRefresh: true);
                
                if (_leagueState.IsLoaded &&
                    _leagueState.League?.Settings?.LastScoredLeg is not null &&
                    _leagueState.League.LeagueId is not null)
                {
                    var lastWeek = Math.Max(1, _leagueState.League.Settings.LastScoredLeg); //If 0 use week 1 instead

                    for(int i = lastWeek; i > 0; i--)
                    {
                        var transactions = await _sleeperApi.GetTransactionsForWeekAsync(leagueId, i.ToString());

                        if (transactions is not null)
                        {
                            Transactions.AddRange(transactions);
                        }
                    }
                    
                }
                leagueId = await _leagueState.GetPreviousLeagueId(leagueId);
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
    public async Task FilterTransactionsData(string type = "")
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
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    public async Task BuildLookupCachesAsync()
    {

    }
}
