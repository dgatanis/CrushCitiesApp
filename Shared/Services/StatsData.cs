using Shared.Models;

namespace Shared.Services;

public sealed class StatsData(LeagueState leagueState, MatchupState matchupState, TransactionState transactionState)
{
    private readonly LeagueState _leagueState = leagueState;
    private readonly MatchupState _matchupState = matchupState;
    private readonly TransactionState _transactionState = transactionState;

    private List<MatchupModel>? Matchups = new();
    private IReadOnlyList<TransactionsModel>? Transactions;
    private Task? _cacheTask;

    /// <summary>
    /// List of MatchupModels of the top 10 highest scoring teams ordered by points.
    /// </summary>
    public List<MatchupModel> TopScoringTeams = new();

    /// <summary>
    /// List of MatchupModels of the top 10 lowest scoring teams ordered by points.
    /// </summary>
    public List<MatchupModel> LowScoringTeams = new();

    /// <summary>
    /// List of MatchupModels of the top 10 highest scoring players order by player points.
    /// </summary>
    public List<MatchupModel> TopScoringPlayers = new();

    /// <summary>
    /// List of MatchupModels of the top 10 lowest scoring players order by player points.
    /// </summary>
    public List<MatchupModel> LowScoringPlayers = new();

    /// <summary>
    /// Dictionary of all-time record by roster id in W-L format.
    /// </summary>
    public Dictionary<int, string> RecordByRosterId = new();

    /// <summary>
    /// Dictionary of total weeks with highest score by roster id.
    /// </summary>
    public Dictionary<int, int> HighScoringWeeksByRosterId = new();

    /// <summary>
    /// Dictionary of total weeks with lowest score by roster id.
    /// </summary>
    public Dictionary<int, int> LowScoringWeeksByRosterId = new();

    /// <summary>
    /// List of MatchupModels and margin for the top 10 largest margins of victory.
    /// </summary>
    public List<KeyValuePair<MatchupModel, double>> LargestMarginOfVictory = new();

    /// <summary>
    /// List of MatchupModels and margin for the top 10 tightest margins of victory.
    /// </summary>
    public List<KeyValuePair<MatchupModel, double>> TightestMarginOfVictory = new();


    /// <summary>
    /// Ensures the cached lookups are loaded
    /// </summary>
    public bool IsCacheLoaded => TopScoringTeams.Count > 0 && LowScoringTeams.Count > 0;
    


    /// <summary>
    /// Ensures the data is loaded that is required for Stat generation
    /// </summary>
    /// <param name="league_id"></param>
    /// <param name="forceRefresh"></param>
    /// <returns></returns>
    private async Task EnsureRequiredDataLoadedAsync()
    {
        await _leagueState.EnsureLoadedAsync();
        await _transactionState.EnsureLoadedAsync();
        await _matchupState.EnsureLoadedAsync();

        Matchups = _matchupState.AllMatchups;
        Transactions = _transactionState.GetFilterTransactionsData(["trade"]);
        
    }
        
    
    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    private async Task BuildLookupCachesAsync()
    {
        await EnsureRequiredDataLoadedAsync();
        
        TopScoringTeams.Clear();
        LowScoringTeams.Clear();
        TopScoringPlayers.Clear();
        LowScoringPlayers.Clear();
        RecordByRosterId.Clear();
        HighScoringWeeksByRosterId.Clear();
        LowScoringWeeksByRosterId.Clear();
        LargestMarginOfVictory.Clear();
        TightestMarginOfVictory.Clear();

        BuildTopScoringTeams();
        BuildLowScoringTeams();
        BuildTopScoringPlayers();
        BuildLowScoringPlayers();
        BuildRecordByRosterId();
        BuildHighScoringWeeksByRosterId();
        BuildLowScoringWeeksByRosterId();
        BuildLargestMarginOfVictory();
        BuildTightestMarginOfVictory();
    }


    /// <summary>
    /// Sets the top 10 highest scoring teams
    /// </summary>
    private void BuildTopScoringTeams()
    {
        TopScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderByDescending(m => m.Points)
                        .Take(10)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 lowest scoring teams
    /// </summary>
    private void BuildLowScoringTeams()
    {
        LowScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderBy(m => m.Points)
                        .Take(10)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 highest scoring players
    /// </summary>
    private void BuildTopScoringPlayers()
    {
        TopScoringPlayers = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .Select(m => new
                            {
                                Matchup = m,
                                TopStarterPoints = (m.Starters is null || m.PlayersPoints is null)
                                    ? 0
                                    : m.PlayersPoints
                                        .Where(kv => m.Starters.Contains(kv.Key))
                                        .Select(kv => kv.Value)
                                        .DefaultIfEmpty(0)
                                        .Max()
                            })
                        .OrderByDescending(x => x.TopStarterPoints)
                        .Take(10)
                        .Select(x => x.Matchup)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 lowest scoring players
    /// </summary>
    private void BuildLowScoringPlayers()
    {
        LowScoringPlayers = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .Select(m => new
                            {
                                Matchup = m,
                                LowestStarterPoints = (m.Starters is null || m.PlayersPoints is null)
                                    ? 0
                                    : m.PlayersPoints
                                        .Where(kv => m.Starters.Contains(kv.Key))
                                        .Select(kv => kv.Value)
                                        .DefaultIfEmpty(0)
                                        .Min()
                            })
                        .OrderBy(x => x.LowestStarterPoints)
                        .Take(10)
                        .Select(x => x.Matchup)
                        .ToList();
    }


    /// <summary>
    /// Calculates and sets the RecordByRosterId variable.
    /// </summary>
    private void BuildRecordByRosterId()
    {
        var winsByRosterId = new Dictionary<int, int>();
        var lossesByRosterId = new Dictionary<int, int>();

        var groupedMatchups = (Matchups ?? Enumerable.Empty<MatchupModel>())
                            .Where(m => m.LeagueId is not null && m.Week is not null && m.MatchupId is not null)
                            .GroupBy(m => new { m.LeagueId, m.Week, m.MatchupId });
        var playoffStartByLeagueId = _leagueState.AllLeagues
                                    .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
                                    .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);

        foreach (var group in groupedMatchups)
        {
            var maxPoints = group.Max(m => m.Points);
            var winners = group.Where(m => m.Points == maxPoints).ToList();
            var losers = group.Where(m => m.Points != maxPoints).ToList();
            
            foreach (var winner in winners)
            {
                if (winner.LeagueId is not null)
                {
                    var playoffWeek = playoffStartByLeagueId.TryGetValue(winner.LeagueId, out var playoffWeekStart) ? playoffWeekStart : 0;

                    if(playoffWeek <= Convert.ToInt32(winner.Week)) continue;
                    if (winsByRosterId.TryGetValue(winner.RosterId, out var wins))
                    {
                        winsByRosterId[winner.RosterId] = wins + 1;
                    }
                    else
                    {
                        winsByRosterId[winner.RosterId] = 1;
                    }
                }
            }

            foreach (var loser in losers)
            {
                if (loser.LeagueId is not null)
                {
                    var playoffWeek = playoffStartByLeagueId.TryGetValue(loser.LeagueId, out var playoffWeekStart) ? playoffWeekStart : 0;
                    if(playoffWeek <= Convert.ToInt32(loser.Week)) continue;
                    if (lossesByRosterId.TryGetValue(loser.RosterId, out var losses))
                    {
                        lossesByRosterId[loser.RosterId] = losses + 1;
                    }
                    else
                    {
                        lossesByRosterId[loser.RosterId] = 1;
                    }
                }
            }
        }

        foreach (var rosterId in winsByRosterId.Keys.Union(lossesByRosterId.Keys))
        {
            winsByRosterId.TryGetValue(rosterId, out var wins);
            lossesByRosterId.TryGetValue(rosterId, out var losses);
            RecordByRosterId[rosterId] = $"{wins}-{losses}";
        }
    }

    /// <summary>
    /// Calculates and sets HighScoringWeeksByRosterId.
    /// </summary>
    private void BuildHighScoringWeeksByRosterId()
    {
        var playoffStartByLeagueId = _leagueState.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);

        var allRosterIds = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Select(m => m.RosterId)
            .Distinct();

        foreach (var rosterId in allRosterIds)
        {
            HighScoringWeeksByRosterId[rosterId] = 0;
        }

        var groupedByWeek = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null)
            .GroupBy(m => new { m.LeagueId, m.Week });

        foreach (var group in groupedByWeek)
        {
            if (!playoffStartByLeagueId.TryGetValue(group.Key.LeagueId!, out var playoffWeekStart))
            {
                continue;
            }

            if (!int.TryParse(group.Key.Week, out var week) || week >= playoffWeekStart)
            {
                continue;
            }

            var maxPoints = group.Max(m => m.Points);
            foreach (var winner in group.Where(m => m.Points == maxPoints))
            {
                if (HighScoringWeeksByRosterId.TryGetValue(winner.RosterId, out var count))
                {
                    HighScoringWeeksByRosterId[winner.RosterId] = count + 1;
                }
                else
                {
                    HighScoringWeeksByRosterId[winner.RosterId] = 1;
                }
            }
        }
    }

    /// <summary>
    /// Calculates and sets LowScoringWeeksByRosterId.
    /// </summary>
    private void BuildLowScoringWeeksByRosterId()
    {
        var playoffStartByLeagueId = _leagueState.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);

        var allRosterIds = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Select(m => m.RosterId)
            .Distinct();

        foreach (var rosterId in allRosterIds)
        {
            LowScoringWeeksByRosterId[rosterId] = 0;
        }

        var groupedByWeek = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null)
            .GroupBy(m => new { m.LeagueId, m.Week });

        foreach (var group in groupedByWeek)
        {
            if (!playoffStartByLeagueId.TryGetValue(group.Key.LeagueId!, out var playoffWeekStart))
            {
                continue;
            }

            if (!int.TryParse(group.Key.Week, out var week) || week >= playoffWeekStart)
            {
                continue;
            }

            var minPoints = group.Min(m => m.Points);
            foreach (var loser in group.Where(m => m.Points == minPoints))
            {
                if (LowScoringWeeksByRosterId.TryGetValue(loser.RosterId, out var count))
                {
                    LowScoringWeeksByRosterId[loser.RosterId] = count + 1;
                }
                else
                {
                    LowScoringWeeksByRosterId[loser.RosterId] = 1;
                }
            }
        }
    }

    /// <summary>
    /// Calculates and sets the top 10 largest margins of victory.
    /// </summary>
    private void BuildLargestMarginOfVictory()
    {
        var groupedMatchups = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null && m.MatchupId is not null)
            .GroupBy(m => new { m.LeagueId, m.Week, m.MatchupId });

        var winnersWithMargin = new List<KeyValuePair<MatchupModel, double>>();

        foreach (var group in groupedMatchups)
        {
            var ordered = group.OrderByDescending(m => m.Points).ToList();
            if (ordered.Count == 0) continue;

            var topPoints = ordered[0].Points;
            var secondPoints = ordered.Count > 1 ? ordered[1].Points : 0;
            var margin = topPoints - secondPoints;

            foreach (var winner in ordered.Where(m => m.Points == topPoints))
            {
                winnersWithMargin.Add(new KeyValuePair<MatchupModel, double>(winner, margin));
            }
        }

        LargestMarginOfVictory = winnersWithMargin
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Calculates and sets the top 10 tightest margins of victory.
    /// </summary>
    private void BuildTightestMarginOfVictory()
    {
        var groupedMatchups = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null && m.MatchupId is not null)
            .GroupBy(m => new { m.LeagueId, m.Week, m.MatchupId });

        var winnersWithMargin = new List<KeyValuePair<MatchupModel, double>>();

        foreach (var group in groupedMatchups)
        {
            var ordered = group.OrderByDescending(m => m.Points).ToList();
            if (ordered.Count == 0) continue;

            var topPoints = ordered[0].Points;
            var secondPoints = ordered.Count > 1 ? ordered[1].Points : 0;
            var margin = topPoints - secondPoints;

            foreach (var winner in ordered.Where(m => m.Points == topPoints))
            {
                winnersWithMargin.Add(new KeyValuePair<MatchupModel, double>(winner, margin));
            }
        }

        TightestMarginOfVictory = winnersWithMargin
            .OrderBy(x => x.Value)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Ensures the cached data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureCacheLoadedAsync()
    {
        if (IsCacheLoaded) return Task.CompletedTask;
        _cacheTask ??= BuildLookupCachesAsync();
        return _cacheTask;
    }

}
