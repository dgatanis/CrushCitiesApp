using Microsoft.Extensions.Logging;
using Shared.Models;

namespace Shared.Services;

/// <summary>
/// Service that stores all-time matchup information.
/// </summary>
/// <param name="sleeperApi"></param>
/// <param name="leagueData"></param>
/// <param name="logger"></param>
public sealed class MatchupData(ISleeperAPI sleeperApi, LeagueData leagueData, ILogger<MatchupData> logger)
{
    private readonly ISleeperAPI _sleeperApi = sleeperApi;
    private readonly LeagueData _leagueData = leagueData;
    private readonly ILogger<MatchupData> _logger = logger;
    private sealed record PlayoffKey(string LeagueId, string Week, int MatchupId);
    private Task? _loadTask;
    private bool _dataLoaded = false;
    
    private List<MatchupModel> _allMatchups = new List<MatchupModel>();

    /// <summary>
    /// List of MatchupModels. Verify using EnsureLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<MatchupModel> AllMatchups => _allMatchups;

    /// <summary>
    /// Ensures AllMatchups is loaded
    /// </summary>
    private bool IsLoaded => _dataLoaded;


    /// <summary>
    /// Sets All Matchups starting from the currentLeagueId looping backwards using the PreviousLeagueId.
    /// </summary>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task SetAllMatchupsAsync(bool forceRefresh = false)
    {
        try
        {
            if(!IsLoaded || forceRefresh)
            {
                ClearAllMatchups();
                await _leagueData.EnsureLoadedAsync();

                var playoffKeys = new HashSet<PlayoffKey>();
                var playoffStartsByLeague = new Dictionary<string, int>();
                
                // Creates keys for playoff matchups based on the playoff brackets for each league
                foreach (var league in _leagueData.AllLeagues)
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
                foreach(var league in _leagueData.AllLeagues)
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

                                _allMatchups?.AddRange(newItems);
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
                    
                    _allMatchups = AllMatchups.Where(m =>
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

            _dataLoaded = true;
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
    /// Clears all of the matchups
    /// </summary>
    public void ClearAllMatchups()
    {
        _allMatchups = [];
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
