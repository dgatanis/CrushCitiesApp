using Shared.Models;
using Shared.Pages;
using System.Globalization;

namespace Shared.Services;

public sealed class StatsData(LeagueState leagueState, MatchupState matchupState, TransactionState transactionState, PlayoffState playoffState)
{
    private readonly LeagueState _leagueState = leagueState;
    private readonly MatchupState _matchupState = matchupState;
    private readonly TransactionState _transactionState = transactionState;
    private readonly PlayoffState _playoffState = playoffState;
 
    private List<MatchupModel>? Matchups = new();
    private IReadOnlyList<TransactionsModel>? Transactions;
    private Task? _cacheTask;
    private bool _cacheLoaded = false;

    /// <summary>
    /// List of MatchupModels of highest scoring teams ordered by points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> TopScoringTeams = new();

    /// <summary>
    /// List of MatchupModels of lowest scoring teams ordered by points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> LowScoringTeams = new();

    /// <summary>
    /// List of MatchupModels of highest scoring players order by player points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> TopScoringPlayers = new();

    /// <summary>
    /// List of MatchupModels of lowest scoring players order by player points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<MatchupModel> LowScoringPlayers = new();

    /// <summary>
    /// Dictionary of total weeks with highest score by roster id, grouped by season.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<string, Dictionary<int, int>> HighScoringWeeksByRosterId = new();

    /// <summary>
    /// Dictionary of total weeks with lowest score by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<int, int> LowScoringWeeksByRosterId = new();

    /// <summary>
    /// List of MatchupModels and margin for the top 10 largest margins of victory.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<KeyValuePair<MatchupModel, double>> LargestMarginOfVictory = new();

    /// <summary>
    /// List of MatchupModels and margin for the top 10 tightest margins of victory.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public List<KeyValuePair<MatchupModel, double>> TightestMarginOfVictory = new();

    /// <summary>
    /// Lookup dictionary for records per team, per season.
    /// Key is season and the value is a SeasonRecord. 
    /// SeasonRecord contains a dictionary with key of week number and a value of a WeekRecord which contains records per roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// <br />
    /// Example: 
    /// <c>
    /// RecordsBySeason["2024"].WeeksByNumber[9].ByRoster[1]
    /// </c>
    /// </summary>
    public Dictionary<string, SeasonRecord> RecordsBySeason = new();


    /// <summary>
    /// Lookup dictionary for championship count by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<int, int> ChampionshipCountByRosterId = new();


    /// <summary>
    /// Lookup dictionary for finals appearances by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<int, int> FinalsAppearancesByRosterId = new();

    /// <summary>
    /// Lookup dictionary for finals appearances by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<int, int> PlayoffAppearancesByRosterId = new();

    /// <summary>
    /// Lookup dictionary for last place finishes (at end of regular season) by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public Dictionary<int, int> LastPlaceFinishesByRosterId = new();
    


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
        await _playoffState.EnsureLoadedAsync();

        Matchups = _matchupState.AllMatchups;
        Transactions = _transactionState.GetFilteredTransactionsData(["trade"]);
    }
        
    
    /// <summary>
    /// Builds dictionaries to be used for quicker lookups on pages
    /// </summary>
    /// <returns></returns>
    private async Task BuildLookupsAsync()
    {
        try
        {
            await EnsureRequiredDataLoadedAsync();
            BuildTopScoringTeams();
            BuildLowScoringTeams();
            BuildTopScoringPlayers();
            BuildLowScoringPlayers();
            BuildHighScoringWeeksByRosterId();
            BuildLowScoringWeeksByRosterId();
            BuildLargestMarginOfVictory();
            BuildTightestMarginOfVictory();
            BuildRecordsBySeason();
            BuildChampionshipCountersByRosterId();
            BuildPlayoffAppearancesByRosterId();
            BuildLastPlaceFinishesByRosterId();
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
        TopScoringTeams.Clear();
        TopScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderByDescending(m => m.Points)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 lowest scoring teams
    /// </summary>
    private void BuildLowScoringTeams()
    {
        LowScoringTeams.Clear();
        LowScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderBy(m => m.Points)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 highest scoring players
    /// </summary>
    private void BuildTopScoringPlayers()
    {
        TopScoringPlayers.Clear();
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
        LowScoringPlayers.Clear();
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
        HighScoringWeeksByRosterId.Clear();
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
        LowScoringWeeksByRosterId.Clear();
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
        LargestMarginOfVictory.Clear();
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
        TightestMarginOfVictory.Clear();
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
    /// Builds a lookup of a count of championships and finals appearances per roster_id.
    /// </summary>
    private void BuildChampionshipCountersByRosterId()
    {
        ChampionshipCountByRosterId.Clear();
        FinalsAppearancesByRosterId.Clear();

        ChampionshipCountByRosterId = new ()
        {
            { 1, 0 },
            { 2, 1 },
            { 3, 0 },
            { 4, 1 },
            { 5, 0 },
            { 6, 0 },
            { 7, 1 },
            { 8, 0 },
            { 9, 0 },
            { 10, 1 }
        };
        FinalsAppearancesByRosterId = new()
        {
            { 1, 0 },
            { 2, 1 },
            { 3, 2 }, 
            { 4, 1 },
            { 5, 0 },
            { 6, 0 },
            { 7, 1 },
            { 8, 0 },
            { 9, 0 },
            { 10, 3 }
        };
        
        foreach(var bracket in _playoffState.AllWinnersBrackets)
        {
            foreach(var playoff in bracket.Value)
            {
                if(playoff.Round != 3 || playoff.PlacementGame != 1 || !playoff.WinnerId.HasValue || !playoff.LoserId.HasValue ) continue;
            
                var winnerId = playoff.WinnerId.Value;
                var loserId = playoff.LoserId.Value;

                // Finals appearance: both winner and loser made the finals
                FinalsAppearancesByRosterId[winnerId] = FinalsAppearancesByRosterId.TryGetValue(winnerId, out var wFinals)
                    ? wFinals + 1
                    : 1;

                FinalsAppearancesByRosterId[loserId] = FinalsAppearancesByRosterId.TryGetValue(loserId, out var lFinals)
                    ? lFinals + 1
                    : 1;

                // Championship count: only winner
                ChampionshipCountByRosterId[winnerId] = ChampionshipCountByRosterId.TryGetValue(winnerId, out var champs)
                    ? champs + 1
                    : 1;
            }
        }
    }


    /// <summary>
    /// Builds a lookup of a count of playoff appearances per roster_id.
    /// </summary>
    private void BuildPlayoffAppearancesByRosterId()
    {
        PlayoffAppearancesByRosterId.Clear();

        PlayoffAppearancesByRosterId = new ()
        {
            { 1, 3 },
            { 2, 4 },
            { 3, 3 }, 
            { 4, 2 },
            { 5, 1 },
            { 6, 1 },
            { 7, 2 },
            { 8, 3 },
            { 9, 1 },
            { 10, 4 }
        };

        foreach (var bracket in _playoffState.AllWinnersBrackets)
        {
            if (string.IsNullOrWhiteSpace(bracket.Key))
            {
                continue;
            }

            var rosterIdsThisSeason = new HashSet<int>();

            foreach (var playoff in bracket.Value)
            {
                if(!playoff.WinnerId.HasValue) continue;
                if (playoff.Team1.HasValue)
                {
                    rosterIdsThisSeason.Add(playoff.Team1.Value);
                }

                if (playoff.Team2.HasValue)
                {
                    rosterIdsThisSeason.Add(playoff.Team2.Value);
                }
            }

            // Count each roster once per season
            foreach (var rosterId in rosterIdsThisSeason)
            {
                PlayoffAppearancesByRosterId[rosterId] = PlayoffAppearancesByRosterId.TryGetValue(rosterId, out var count)
                    ? count + 1
                    : 1;
            }
        }
    }


    /// <summary>
    /// Builds a lookup of a count of last place finishes per roster_id.
    /// </summary>
    private void BuildLastPlaceFinishesByRosterId()
    {
        LastPlaceFinishesByRosterId.Clear();

        LastPlaceFinishesByRosterId = new ()
        {
            { 1, 0 },
            { 2, 0 },
            { 3, 0 },
            { 4, 2 },
            { 5, 0 },
            { 6, 0 },
            { 7, 1 },
            { 8, 1 },
            { 9, 0 },
            { 10, 0}
        };

        var playoffStartByLeagueId = _leagueState.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);

        var records = RecordsBySeason ?? new Dictionary<string, SeasonRecord>();

        foreach (var seasonRecord in records.Values)
        {
            foreach (var weekRecord in seasonRecord.WeeksByNumber.Values)
            {
                var leagueId = _leagueState.AllLeagues.FirstOrDefault(l => l.Season == seasonRecord.Season)?.LeagueId;
                if (leagueId is null || !playoffStartByLeagueId.TryGetValue(leagueId, out var playoffWeekStart))
                {
                    continue;
                }

                if (weekRecord.WeekNumber >= playoffWeekStart)
                {
                    continue;
                }
                
                if(weekRecord.WeekNumber == playoffWeekStart -1)
                {
                    var bottomEntry = weekRecord.ByRoster
                                    .OrderBy(kv => kv.Value.Wins)
                                    .ThenBy(kv => double.TryParse(kv.Value.PointsFor, NumberStyles.Any, CultureInfo.InvariantCulture, out var pf)
                                        ? pf
                                        : double.MaxValue)
                                    .First();


                    LastPlaceFinishesByRosterId[bottomEntry.Key] = LastPlaceFinishesByRosterId.TryGetValue(bottomEntry.Key, out var count)
                    ? count + 1
                    : 1;
                }
            }
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
    /// Ensures the lookup data is loaded.
    /// </summary>
    /// <returns></returns>
    public Task EnsureLookupsLoadedAsync()
    {
        if (IsCacheLoaded) return Task.CompletedTask;
        _cacheTask ??= BuildLookupsAsync();
        return _cacheTask;
    }
}