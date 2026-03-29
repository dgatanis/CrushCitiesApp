using Shared.Models;

namespace Shared.Services;

public sealed class MatchupState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private Task? _loadTask;

    private sealed record PlayoffKey(string LeagueId, string Week, int RosterId);

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
    /// Sets All Matchups starting from the currentLeagueId looping backwards using the PreviousLeagueId.
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    public async Task SetAllMatchupsAsync(bool forceRefresh = false)
    {
        try
        {
            if(!IsLoaded || forceRefresh)
            {
                ClearAllMatchups();
                await _leagueState.EnsureLoadedAsync();

                var playoffKeys = new HashSet<PlayoffKey>();
                var playoffStartsByLeague = new Dictionary<string, int>();

                foreach (var league in _leagueState.AllLeagues)
                {
                    if (league.LeagueId is null) continue;
                    if (league.Settings?.PlayoffWeekStart is int playoffStart)
                    {
                        playoffStartsByLeague[league.LeagueId] = playoffStart;
                    }

                    var winnersBracket = await _sleeperApi.GetPlayoffWinnersBracketAsync(league.LeagueId);
                    var losersBracket = await _sleeperApi.GetPlayoffLosersBracketAsync(league.LeagueId);

                    void AddBracketKeys(List<PlayoffBracketsModel>? brackets)
                    {
                        if (brackets is null || brackets.Count == 0) return;
                        if (!playoffStartsByLeague.TryGetValue(league.LeagueId, out var start)) return;

                        foreach (var bracket in brackets)
                        {
                            if (bracket.Round is < 1 or > 3) continue;
                            if (bracket.PlacementGame == 5 || bracket.PlacementGame == 3) continue;

                            var week = (start + (bracket.Round - 1)).ToString();
                            if (bracket.Team1 is int t1)
                            {
                                playoffKeys.Add(new PlayoffKey(league.LeagueId, week, t1));
                            }
                            if (bracket.Team2 is int t2)
                            {
                                playoffKeys.Add(new PlayoffKey(league.LeagueId, week, t2));
                            }
                        }
                    }

                    AddBracketKeys(winnersBracket);
                    AddBracketKeys(losersBracket);
                }

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
                                var existing = new HashSet<(string? Season, string? Week, int? MatchupId)>(
                                    AllMatchups is not null && AllMatchups.Select(a => (a.Season, a.Week, a.MatchupId)) is not null 
                                    ? AllMatchups.Select(a => (a.Season, a.Week, a.MatchupId)) 
                                    : new List<(string?, string?, int?)>()
                                );
                                var newItems = matchups
                                    .Select(m =>
                                    {
                                        m.Season = season;
                                        m.Week = i.ToString();
                                        m.LeagueId = currentLeagueId;
                                        return m;
                                    })
                                    .Where(m => existing.Add((m.Season, m.Week, m.MatchupId)))
                                    .OrderBy(m => m.MatchupId);

                                AllMatchups?.AddRange(newItems);
                            }
                        }
                    }
                }

                if (AllMatchups is not null && AllMatchups.Count > 0)
                {
                    AllMatchups = AllMatchups.Where(m =>
                    {
                        if (m.LeagueId is null) return false;
                        if (!playoffStartsByLeague.TryGetValue(m.LeagueId, out var start)) return true;
                        if (!int.TryParse(m.Week, out var week)) return true;

                        if (week <= start) return true;

                        return playoffKeys.Contains(new PlayoffKey(m.LeagueId, m.Week ?? "", m.RosterId));
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            _loadTask = null;
            Console.WriteLine($"ERROR: {ex.Message}");
            throw;
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
