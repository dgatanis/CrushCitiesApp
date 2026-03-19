using Shared.Models;

namespace Shared.Services;

public sealed class MatchupState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;

    
    public List<MatchupModel>? AllMatchups { get; set; } = new();
    public bool IsLoadedAllMatchups { get; private set; } = false;


    /// <summary>
    /// Sets All Matchups starting from the currentLeagueId looping backwards using the PreviousLeagueId
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllMatchupsAsync(bool forceRefresh = false)
    {
        if(forceRefresh) ClearAllMatchups();
        if(!_leagueState.IsLoadedAllLeagues) await _leagueState.SetAllLeaguesDataAsync();

        foreach(var league in _leagueState.AllLeagues)
        {
            if (league.Settings?.LastScoredLeg is not null &&
                league.LeagueId is not null)
            {
                var currentLeagueId = league.LeagueId;
                var season = league.Season ?? "";
                var lastWeek = league.Settings.LastScoredLeg;

                for(int i = lastWeek; i > 0; i--)
                {
                    var matchups = await _sleeperApi.GetMatchupsForWeekAsync(league.LeagueId, i.ToString());
                    
                    if (matchups is not null)
                    {
                        AllMatchups?.AddRange(matchups
                            .Where(m => !AllMatchups.Any(a => a.MatchupId == m.MatchupId && a.Season == m.Season && a.Week == m.Week))
                            .Select(m =>
                            {
                                m.Season = season;
                                m.Week = i.ToString();
                                m.LeagueId = currentLeagueId;
                                return m;
                            }).OrderBy(m => m.MatchupId).ToList());
                    }
                }
            }
        }
        IsLoadedAllMatchups = true;
    }


    /// <summary>
    /// Clears all of the matchups
    /// </summary>
    public void ClearAllMatchups()
    {
        AllMatchups = [];
        IsLoadedAllMatchups = false;
    }

}
