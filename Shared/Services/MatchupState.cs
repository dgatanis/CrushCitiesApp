using Shared.Models;

namespace Shared.Services;

public sealed class MatchupState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;

    
    public List<MatchupModel>? AllMatchups { get; set; } = new();
    public bool IsLoadedAllMatchups { get; set; } = false;


    /// <summary>
    /// Sets All Matchups starting from the currentLeagueId looping backwards using the PreviousLeagueId
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllMatchups(bool forceRefresh = false)
    {
        if(forceRefresh) ClearAllMatchups();
        var leagueId = await _leagueState.GetCurrentLeagueId();

        while (!string.IsNullOrWhiteSpace(leagueId))
        {
            await _leagueState.SetLeague(leagueId, forceRefresh: true);

            if (_leagueState.IsLoaded &&
                _leagueState.League?.Settings?.LastScoredLeg is not null &&
                _leagueState.League.LeagueId is not null)
            {
                var currentLeagueId = _leagueState.League.LeagueId;
                var season = _leagueState.League.Season ?? "";
                var lastWeek = _leagueState.League.Settings.LastScoredLeg;

                for(int i = lastWeek; i > 0; i--)
                {
                    var matchups = await _sleeperApi.GetMatchupsForWeekAsync(leagueId, i.ToString());

                    if (matchups is not null)
                    {
                        AllMatchups?.AddRange(matchups
                            .Where(m => !AllMatchups.Any(a => a.MatchupId == m.MatchupId && a.Season == m.Season && a.Week == m.Week))
                            .Select(m =>
                            {
                                m.Season = season;
                                m.Week = i.ToString();
                                return m;
                            }).OrderBy(m => m.MatchupId).ToList());
                    }
                }
                leagueId = await _leagueState.GetPreviousLeagueId(leagueId);
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
