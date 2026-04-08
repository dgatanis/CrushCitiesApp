using Microsoft.Extensions.Logging;
using Shared.Models;

namespace Shared.Services;


/// <summary>
/// Service that stores the playoff brackets for the current league and builds lookups for frequently accessed data.
/// </summary>
/// <param name="sleeperApi"></param>
/// <param name="leagueState"></param>
/// <param name="matchupState"></param>
/// <param name="logger"></param>
public sealed class PlayoffState(ISleeperAPI sleeperApi, LeagueState leagueState, MatchupState matchupState, ILogger<PlayoffState> logger)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private readonly MatchupState _matchupState = matchupState;
    private readonly ILogger<PlayoffState> _logger = logger;
    private Task? _loadTask;
    private Task? _lookupTask;
    private bool _dataLoaded = false;
    private bool _lookupsLoaded = false;
    
    private Dictionary<string, List<PlayoffBracketsModel>> _allWinnersBrackets  = new();
    private Dictionary<string, List<PlayoffBracketsModel>> _allLosersBrackets  = new();
    private List<MatchupModel> _winnersBracketMatchups = new();
    private List<MatchupModel> _losersBracketMatchups = new();
    
    /// <summary>
    /// Dictionary of all the winner brackets (PlayoffBracketsModel). 
    /// Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<string, List<PlayoffBracketsModel>> AllWinnersBrackets => _allWinnersBrackets;

    /// <summary>
    /// Dictionary of all the loser brackets (PlayoffBracketsModel). 
    /// Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<string, List<PlayoffBracketsModel>> AllLosersBrackets => _allLosersBrackets;

    /// <summary>
    /// List of the matchups that were part of the winner bracket playoffs. 
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<MatchupModel> WinnersBracketMatchups => _winnersBracketMatchups;

    /// <summary>
    /// List of the matchups that were part of the loser bracket playoffs.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<MatchupModel> LosersBracketMatchups => _losersBracketMatchups;
    
    /// <summary>
    /// Ensures lookups are loaded
    /// </summary>
    private bool IsLookupLoaded => _lookupsLoaded;

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
                _allWinnersBrackets.Clear();
                _allLosersBrackets.Clear();
                var league_id = await _leagueState.GetCurrentLeagueIdAsync();

                while (!string.IsNullOrWhiteSpace(league_id))
                {
                    var winnersBracket = await _sleeperApi.GetPlayoffWinnersBracketAsync(league_id);
                    var losersBracket = await _sleeperApi.GetPlayoffLosersBracketAsync(league_id);

                    if (winnersBracket is {Count: > 0} && losersBracket is {Count: > 0})
                    {
                        _allWinnersBrackets.Add(league_id, winnersBracket);
                        _allLosersBrackets.Add(league_id, losersBracket);
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
            await _matchupState.EnsureLoadedAsync();
            await _leagueState.EnsureLoadedAsync();

            _winnersBracketMatchups.Clear();
            _losersBracketMatchups.Clear();

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
                            _winnersBracketMatchups.AddRange(matchups.ToList());
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
                            _losersBracketMatchups.AddRange(matchups.ToList());
                        }
                    }
                }
                
            }

            _lookupsLoaded = true;
        }
        catch (Exception ex)
        {
            _lookupTask = null;
            _lookupsLoaded = false;
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
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
        if (IsLookupLoaded) return Task.CompletedTask;
        _lookupTask ??= BuildLookupsAsync();
        return _lookupTask;
    }
}

