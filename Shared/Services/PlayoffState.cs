using System.Security.Cryptography.X509Certificates;
using Shared.Models;

namespace Shared.Services;

public sealed class PlayoffState(ISleeperAPI sleeperApi, LeagueState leagueState, MatchupState matchupState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private readonly MatchupState _matchupState = matchupState;
    
    public Dictionary<string, List<PlayoffBracketsModel>> AllWinnersBrackets  = new();
    public Dictionary<string, List<PlayoffBracketsModel>> AllLosersBrackets = new();
    public List<MatchupModel> WinnersBracketMatchups { get; private set; } = new();
    public List<MatchupModel> LosersBracketMatchups { get; private set; } = new();

    public bool IsLoaded => AllWinnersBrackets.Count > 0 && AllLosersBrackets.Count > 0;


    /// <summary>
    /// Sets the roster based on the leagueid
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllPlayoffDataAsync(bool forceRefresh = false)
    {
        if (!IsLoaded || forceRefresh)
        {
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
        await BuildLookupCachesAsync();
    }


    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    public async Task BuildLookupCachesAsync()
    {
        if(!_matchupState.IsLoadedAllMatchups) await _matchupState.SetAllMatchupsAsync();
        if(!_leagueState.IsLoadedAllLeagues) await _leagueState.SetAllLeaguesDataAsync();
        foreach(var list in AllWinnersBrackets)
        {
            var league_id = list.Key;
            
            foreach(var bracket in list.Value)
            {
                Console.WriteLine($"TESTING123 - {_leagueState.AllLeagues.Count}");
                if(_leagueState.AllLeagues.Count > 0)
                {
                    Console.WriteLine($"TESTING123 {_leagueState.AllLeagues.FirstOrDefault(l => l.LeagueId == league_id)}");
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
    }
    
}
