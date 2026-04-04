using Shared.Models;
using System.Globalization;

namespace Shared.Services;

public sealed class StatsData(LeagueState leagueState, MatchupState matchupState, TransactionState transactionState)
{
    private readonly LeagueState _leagueState = leagueState;
    private readonly MatchupState _matchupState = matchupState;
    private readonly TransactionState _transactionState = transactionState;

    private List<MatchupModel>? Matchups = new();
    private IReadOnlyList<TransactionsModel>? Transactions;
    private Task? _cacheTask;
    private bool _cacheLoaded = false;

    /// <summary>
    /// List of MatchupModels of highest scoring teams ordered by points.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> TopScoringTeams = new();

    /// <summary>
    /// List of MatchupModels of lowest scoring teams ordered by points.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> LowScoringTeams = new();

    /// <summary>
    /// List of MatchupModels of highest scoring players order by player points.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> TopScoringPlayers = new();

    /// <summary>
    /// List of MatchupModels of lowest scoring players order by player points.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> LowScoringPlayers = new();

    /// <summary>
    /// Dictionary of total weeks with highest score by roster id, grouped by season.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<string, Dictionary<int, int>> HighScoringWeeksByRosterId = new();

    /// <summary>
    /// Dictionary of total weeks with lowest score by roster id.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<int, int> LowScoringWeeksByRosterId = new();

    /// <summary>
    /// List of MatchupModels and margin for the top 10 largest margins of victory.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public List<KeyValuePair<MatchupModel, double>> LargestMarginOfVictory = new();

    /// <summary>
    /// List of MatchupModels and margin for the top 10 tightest margins of victory.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// </summary>
    public List<KeyValuePair<MatchupModel, double>> TightestMarginOfVictory = new();

    /// <summary>
    /// Lookup dictionary for records per team, per season.
    /// Key is season and the value is a SeasonRecord. 
    /// SeasonRecord contains a dictionary with key of week number and a value of a WeekRecord which contains records per roster id.
    /// Verify using EnsureCacheLoadedAsync() before accessing.
    /// <br />
    /// Example: 
    /// <c>
    /// RecordsBySeason["2024"].WeeksByNumber[9].ByRoster[1]
    /// </c>
    /// </summary>
    public Dictionary<string, SeasonRecord> RecordsBySeason = new();


    /// <summary>
    /// Ensures the lookups are loaded
    /// </summary>
    private bool IsCacheLoaded => _cacheLoaded;
    


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
        Transactions = _transactionState.GetFilteredTransactionsData(["trade"]);
    }
        
    
    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    private async Task BuildLookupCachesAsync()
    {
        try
        {
            await EnsureRequiredDataLoadedAsync();
        
            TopScoringTeams.Clear();
            LowScoringTeams.Clear();
            TopScoringPlayers.Clear();
            LowScoringPlayers.Clear();
            HighScoringWeeksByRosterId.Clear();
            LowScoringWeeksByRosterId.Clear();
            LargestMarginOfVictory.Clear();
            TightestMarginOfVictory.Clear();
            RecordsBySeason.Clear();

            BuildTopScoringTeams();
            BuildLowScoringTeams();
            BuildTopScoringPlayers();
            BuildLowScoringPlayers();
            BuildHighScoringWeeksByRosterId();
            BuildLowScoringWeeksByRosterId();
            BuildLargestMarginOfVictory();
            BuildTightestMarginOfVictory();
            BuildRecordsBySeason();
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
    /// Sets the top 10 highest scoring teams
    /// </summary>
    private void BuildTopScoringTeams()
    {
        TopScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderByDescending(m => m.Points)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 lowest scoring teams
    /// </summary>
    private void BuildLowScoringTeams()
    {
        LowScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderBy(m => m.Points)
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
                        .Select(x => x.Matchup)
                        .ToList();
    }


    /// <summary>
    /// Sets the lowest scoring players per matchup.
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
                        .Select(x => x.Matchup)
                        .ToList();
    }

    /// <summary>
    /// Calculates and sets HighScoringWeeksByRosterId per season.
    /// </summary>
    private void BuildHighScoringWeeksByRosterId()
    {
        var playoffStartByLeagueId = _leagueState.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);
        var allRosterIds = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Select(m => m.RosterId)
            .Distinct()
            .ToList();
        var seasons = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => !string.IsNullOrWhiteSpace(m.Season))
            .Select(m => m.Season!)
            .Distinct()
            .ToList();
        var groupedByWeek = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.LeagueId is not null && m.Week is not null && !string.IsNullOrWhiteSpace(m.Season))
            .GroupBy(m => new { m.LeagueId, m.Week, Season = m.Season! });

        foreach (var season in seasons)
        {
            if (!HighScoringWeeksByRosterId.TryGetValue(season, out var rosterCounts))
            {
                rosterCounts = new Dictionary<int, int>();
                HighScoringWeeksByRosterId[season] = rosterCounts;
            }

            foreach (var rosterId in allRosterIds)
            {
                if (!rosterCounts.ContainsKey(rosterId))
                {
                    rosterCounts[rosterId] = 0;
                }
            }
        }

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
                if (!HighScoringWeeksByRosterId.TryGetValue(group.Key.Season, out var rosterCounts))
                {
                    rosterCounts = new Dictionary<int, int>();
                    HighScoringWeeksByRosterId[group.Key.Season] = rosterCounts;
                }

                if (rosterCounts.TryGetValue(winner.RosterId, out var count))
                {
                    rosterCounts[winner.RosterId] = count + 1;
                }
                else
                {
                    rosterCounts[winner.RosterId] = 1;
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
            .ToList();
    }


    /// <summary>
    /// Builds a lookup of records by season/ week / rosterid 
    /// </summary>
    private void BuildRecordsBySeason()
    {
        RecordsBySeason.Clear();
        if(Matchups is null || Matchups.Count == 0) return;

        var seasonGroups = Matchups 
            .Where(m => !string.IsNullOrWhiteSpace(m.Season) && !string.IsNullOrWhiteSpace(m.Week))
            .GroupBy(m => m.Season!);

        foreach (var seasonGroup in seasonGroups)
        {
            var season = seasonGroup.Key;
            var weekGroups = seasonGroup
                .Select(m => new { Matchup = m, WeekNumber = int.TryParse(m.Week, out var num) ? num : -1 })
                .Where(x => x.WeekNumber != -1)
                .GroupBy(x => x.WeekNumber!)
                .OrderBy(g => g.Key);
            var cumulative = new Dictionary<int, Record>();
            var cumulativePoints = new Dictionary<int, double>();
            var seasonRecord = new SeasonRecord(season);

            foreach (var weekGroup in weekGroups)
            {
                var matchupsThisWeek = weekGroup.Select(x => x.Matchup);
                foreach (var matchupPartition in matchupsThisWeek.GroupBy(m => m.MatchupId))
                {
                    var winnerRosterId = GetWinnerRosterId(matchupPartition);
                    foreach (var team in matchupPartition)
                    {
                        if (!cumulative.TryGetValue(team.RosterId, out var current))
                        {
                            current = new Record();
                            cumulative[team.RosterId] = current;
                        }
                        if (!cumulativePoints.TryGetValue(team.RosterId, out var totalPoints))
                        {
                            totalPoints = 0;
                        }

                        totalPoints += team.Points;
                        cumulativePoints[team.RosterId] = totalPoints;
                        current.PointsFor = totalPoints.ToString("0.00", CultureInfo.InvariantCulture);

                        if (!winnerRosterId.HasValue)
                        {
                            current.Ties += 1;
                        }
                        else if (winnerRosterId.Value == team.RosterId)
                        {
                            current.Wins += 1;
                        }
                        else
                        {
                            current.Losses += 1;
                        }
                    }
                }

                var weekRecord = new WeekRecord(weekGroup.Key);
                foreach (var entry in cumulative)
                {
                    weekRecord.ByRoster[entry.Key] = new Record
                    {
                        Wins = entry.Value.Wins,
                        Losses = entry.Value.Losses,
                        Ties = entry.Value.Ties,
                        PointsFor = entry.Value.PointsFor
                    };
                }

                seasonRecord.WeeksByNumber[weekGroup.Key] = weekRecord;
            }

            RecordsBySeason[season] = seasonRecord;
        }
    }

    /// <summary>
    /// Calculates the winner based on the matchup
    /// </summary>
    /// <param name="teams"></param>
    /// <returns></returns>
    public static int? GetWinnerRosterId(IEnumerable<MatchupModel> teams)
    {
        var ordered = teams.OrderByDescending(t => t.Points).ToList();
        if (ordered.Count == 0)
        {
            return null;
        }

        var topPoints = ordered[0].Points;
        var topCount = ordered.Count(t => t.Points == topPoints);
        return topCount > 1 ? null : ordered[0].RosterId;
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