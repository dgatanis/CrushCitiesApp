using Shared.Models;

namespace Shared.Services;

public sealed class MatchupState(ISleeperAPI sleeperApi, LeagueState leagueState)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueState _leagueState = leagueState;
    private Task? _loadTask;

    private sealed record PlayoffKey(string LeagueId, string Week, int MatchupId);

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
                
                // Creates keys for playoff matchups based on the playoff brackets for each league
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

                // Loops through each league and adds the matchups
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
                                var seed = AllMatchups is not null
                                    ? AllMatchups.Select(a => (
                                        (string?)a.Season,
                                        (string?)a.Week,
                                        (int?)a.MatchupId,
                                        (int?)a.RosterId
                                    ))
                                    : Enumerable.Empty<(string? Season, string? Week, int? MatchupId, int? RosterId)>();

                                var existing = new HashSet<(string? Season, string? Week, int? MatchupId, int? RosterId)>(seed);

                                var newItems = matchups
                                    .Select(m =>
                                    {
                                        m.Season = season;
                                        m.Week = i.ToString();
                                        m.LeagueId = currentLeagueId;
                                        return m;
                                    })
                                    .Where(m => existing.Add((m.Season, m.Week, m.MatchupId, m.RosterId)))
                                    .OrderBy(m => m.MatchupId);

                                AllMatchups?.AddRange(newItems);
                            }
                        }
                    }
                }

                // Filters out playoff matchups that are not in the playoff brackets we want
                if (AllMatchups is not null && AllMatchups.Count > 0)
                {
                    var playoffMatchupIds = AllMatchups
                        .Where(m => m.LeagueId is not null && m.Week is not null && m.MatchupId is not null)
                        .Where(m => playoffKeys.Contains(new PlayoffKey(m.LeagueId!, m.Week!, m.RosterId)))
                        .Select(m => (m.LeagueId!, m.Week!, m.MatchupId!.Value))
                        .ToHashSet();
                    
                    AllMatchups = AllMatchups.Where(m =>
                    {
                        if (m.LeagueId is null || m.Week is null) return false;
                        if (!playoffStartsByLeague.TryGetValue(m.LeagueId, out var start)) return true;
                        if (!int.TryParse(m.Week, out var week)) return true;

                        if (week <= start) return true;
                        if (m.MatchupId is null) return false;
                        
                        return playoffMatchupIds.Contains((m.LeagueId, m.Week, m.MatchupId.Value));                    
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
