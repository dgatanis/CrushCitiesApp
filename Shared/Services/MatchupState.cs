using Shared.Models;

namespace Shared.Services;

public sealed class MatchupState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private Task? _loadTask;

    /// <summary>
    /// List of all Matchups.
    /// Set via SetAllMatchupsAsync()
    /// </summary>
    public List<MatchupModel>? AllMatchups { get; set; } = new();

    /// <summary>
    /// Ensures AllMatchups is loaded
    /// </summary>
    public bool IsLoaded => AllMatchups is not null && AllMatchups.Count > 0;


    /// <summary>
    /// Sets All Matchups starting from the currentLeagueId looping backwards using the PreviousLeagueId
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllMatchupsAsync(bool forceRefresh = false)
    {
        if(forceRefresh) ClearAllMatchups();
        await _leagueState.EnsureLoadedAsync();

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
    }


    /// <summary>
    /// Clears all of the matchups
    /// </summary>
    public void ClearAllMatchups()
    {
        AllMatchups = [];
    }


    /// <summary>
    /// Ensures that the AllMatchups data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLoadedAsync()
    {
        if (IsLoaded) return Task.CompletedTask;
        _loadTask ??= SetAllMatchupsAsync(forceRefresh: true);
        return _loadTask;
    }

}
