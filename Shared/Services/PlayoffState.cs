using Shared.Models;

namespace Shared.Services;

public sealed class PlayoffState(ISleeperAPI sleeperApi, LeagueState leagueState, MatchupState matchupState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private readonly MatchupState _matchupState = matchupState;
    private Task? _loadTask;
    private Task? _cacheTask;
    private bool _dataLoaded = false;
    private bool _cacheLoaded = false;
    
    /// <summary>
    /// Dictionary of all the winner brackets (PlayoffBracketsModel). 
    /// Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<string, List<PlayoffBracketsModel>> AllWinnersBrackets  = new();

    /// <summary>
    /// Dictionary of all the loser brackets (PlayoffBracketsModel). 
    /// Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<string, List<PlayoffBracketsModel>> AllLosersBrackets = new();

    /// <summary>
    /// List of the matchups that were part of the winner bracket playoffs. 
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> WinnersBracketMatchups { get; private set; } = new();

    /// <summary>
    /// List of the matchups that were part of the loser bracket playoffs.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> LosersBracketMatchups { get; private set; } = new();
    
    /// <summary>
    /// Ensures lookups are loaded
    /// </summary>
    private bool IsCacheLoaded => _cacheLoaded;

    /// <summary>
    /// Ensures AllWinndersBrackets and AllLosersBrackets are loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;


    /// <summary>
    /// Sets all playoff data beginning with the CurrentLeagueId looping backwards through PreviousLeagueIds
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task SetAllPlayoffDataAsync(bool forceRefresh = false)
    {
        try
        {
            if (!IsLoaded || forceRefresh)
            {
                AllWinnersBrackets.Clear();
                AllLosersBrackets.Clear();
                var league_id = await _leagueState.GetCurrentLeagueIdAsync();

                while (!string.IsNullOrWhiteSpace(league_id))
                {
                    var winnersBracket = await _sleeperApi.GetPlayoffWinnersBracketAsync(league_id);
                    var losersBracket = await _sleeperApi.GetPlayoffLosersBracketAsync(league_id);

                    if (winnersBracket is {Count: > 0} && losersBracket is {Count: > 0})
                    {
                        AllWinnersBrackets.Add(league_id, winnersBracket);
                        AllLosersBrackets.Add(league_id, losersBracket);
                    }

                    league_id = await _leagueState.GetPreviousLeagueIdAsync(league_id);
                }
            }
            _dataLoaded = true;
            await BuildLookupsAsync();
        }
        catch (Exception ex)
        {
            _loadTask = null;
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
            await _matchupState.EnsureLoadedAsync();
            await _leagueState.EnsureLoadedAsync();

            WinnersBracketMatchups.Clear();
            LosersBracketMatchups.Clear();

            foreach(var list in AllWinnersBrackets)
            {
                var league_id = list.Key;
                
                foreach(var bracket in list.Value)
                {
                    if(_leagueState.AllLeagues.Count > 0)
                    {
                        var week = _leagueState.AllLeagues.FirstOrDefault(l => l.LeagueId == league_id)?.Settings?.PlayoffWeekStart is int start && bracket.Round is >= 1 and <= 3
                                    ? (start + (bracket.Round - 1)).ToString()
                                    : "";
                        var matchups = _matchupState.AllMatchups is not null 
                                    ? _matchupState.AllMatchups.Where(m => m.Week == week && m.LeagueId == league_id && bracket.PlacementGame != 5 &&
                                                                    (m.RosterId == bracket.Team1 || m.RosterId == bracket.Team2))
                                    : [];

                        if(matchups is not null)
                        {
                            WinnersBracketMatchups.AddRange(matchups.ToList());
                        }
                        
                    }

                }
                
            }

            foreach(var list in AllLosersBrackets)
            {
                var league_id = list.Key;

                foreach(var bracket in list.Value)
                {
                    
                    if(_leagueState.AllLeagues.Count > 0)
                    {
                        var week = _leagueState.AllLeagues.FirstOrDefault(l => l.LeagueId == league_id)?.Settings?.PlayoffWeekStart is int start && bracket.Round is >= 1 and <= 3
                                    ? (start + (bracket.Round - 1)).ToString()
                                    : "";
                        var matchups = _matchupState.AllMatchups is not null 
                                    ? _matchupState.AllMatchups.Where(m => m.Week == week && m.LeagueId == league_id && bracket.PlacementGame != 3 &&
                                                                    (m.RosterId == bracket.Team1 || m.RosterId == bracket.Team2))
                                    : [];

                        if(matchups is not null)
                        {
                            LosersBracketMatchups.AddRange(matchups.ToList());
                        }
                    }
                }
                
            }

            _cacheLoaded = true;
        }
        catch (Exception ex)
        {
            _cacheTask = null;
            _cacheLoaded = false;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ensures that required data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllPlayoffDataAsync(forceRefresh: true);
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
}
