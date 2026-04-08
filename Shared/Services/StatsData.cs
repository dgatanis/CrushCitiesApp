using Microsoft.Extensions.Logging;
using Radzen;
using Shared.Models;
using Shared.Pages;
using System.Globalization;

namespace Shared.Services;

// TODO: Turn this into multiple services its getting too large

/// <summary>
/// Service that calculates and builds lookups for frequently accessed data.
/// </summary>
/// <param name="leagueState"></param>
/// <param name="matchupState"></param>
/// <param name="transactionState"></param>
/// <param name="playoffState"></param>
/// <param name="logger"></param>
public sealed class StatsData(LeagueState leagueState, MatchupState matchupState, TransactionState transactionState, PlayoffState playoffState, ILogger<StatsData> logger)
{
    private readonly LeagueState _leagueState = leagueState;
    private readonly MatchupState _matchupState = matchupState;
    private readonly TransactionState _transactionState = transactionState;
    private readonly PlayoffState _playoffState = playoffState;
    private readonly ILogger<StatsData> _logger = logger;
    private Task? _lookupTask;
    private bool _lookupsLoaded = false;
 
    private IReadOnlyList<MatchupModel>? Matchups;
    private IReadOnlyList<TransactionsModel>? Trades;
    private List<MatchupModel> _topScoringTeams = new();
    private List<MatchupModel> _lowScoringTeams = new();
    private List<MatchupModel> _topScoringPlayers = new();
    private List<MatchupModel> _lowScoringPlayers = new();
    private Dictionary<string, Dictionary<int, int>> _highScoringWeeksByRosterId = new();
    private Dictionary<int, int> _lowScoringWeeksByRosterId = new();
    private List<KeyValuePair<MatchupModel, double>> _largestMarginOfVictory = new();
    private List<KeyValuePair<MatchupModel, double>> _tightestMarginOfVictory = new();
    private Dictionary<string, SeasonRecord> _recordsBySeason = new();
    private Dictionary<int, int> _championshipCountByRosterId = new();
    private Dictionary<int, int> _finalsAppearancesByRosterId = new();
    private Dictionary<int, int> _playoffAppearancesByRosterId = new();
    private Dictionary<int, int> _lastPlaceFinishesByRosterId = new();
    private Dictionary<int, int> _mostTradedWithRosterId = new();
    private Dictionary<int, int> _tradeCountsByRosterId = new();
    private Dictionary<int, int> _mostLossesAgainstRosterId = new();
    private Dictionary<int, int> _mostWinsAgainstRosterId = new();
    private List<(string,Dictionary<int,int>)> _endOfSeasonFinishes = new();


    /// <summary>
    /// List of MatchupModels of highest scoring teams ordered by points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<MatchupModel> TopScoringTeams => _topScoringTeams;

    /// <summary>
    /// List of MatchupModels of lowest scoring teams ordered by points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<MatchupModel> LowScoringTeams => _lowScoringTeams;

    /// <summary>
    /// List of MatchupModels of highest scoring players order by player points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<MatchupModel> TopScoringPlayers => _topScoringPlayers;

    /// <summary>
    /// List of MatchupModels of lowest scoring players order by player points.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<MatchupModel> LowScoringPlayers => _lowScoringPlayers;

    /// <summary>
    /// Dictionary of total weeks with highest score by roster id, grouped by season.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<string, Dictionary<int, int>> HighScoringWeeksByRosterId => _highScoringWeeksByRosterId;

    /// <summary>
    /// Dictionary of total weeks with lowest score by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> LowScoringWeeksByRosterId => _lowScoringWeeksByRosterId;

    /// <summary>
    /// List of MatchupModels and margin for the top 10 largest margins of victory.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<KeyValuePair<MatchupModel, double>> LargestMarginOfVictory => _largestMarginOfVictory;

    /// <summary>
    /// List of MatchupModels and margin for the top 10 tightest margins of victory.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<KeyValuePair<MatchupModel, double>> TightestMarginOfVictory => _tightestMarginOfVictory;

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
    public IReadOnlyDictionary<string, SeasonRecord> RecordsBySeason => _recordsBySeason;

    /// <summary>
    /// Lookup dictionary for championship count by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> ChampionshipCountByRosterId => _championshipCountByRosterId;

    /// <summary>
    /// Lookup dictionary for finals appearances by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> FinalsAppearancesByRosterId => _finalsAppearancesByRosterId;

    /// <summary>
    /// Lookup dictionary for finals appearances by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> PlayoffAppearancesByRosterId => _playoffAppearancesByRosterId;

    /// <summary>
    /// Lookup dictionary for last place finishes (at end of regular season) by roster id.
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> LastPlaceFinishesByRosterId => _lastPlaceFinishesByRosterId;

    /// <summary>
    /// Lookup dictionary for most traded with by roster id (roster_id, roster_id they have traded with the most).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> MostTradedWithRosterId => _mostTradedWithRosterId;
    
    /// <summary>
    /// Lookup dictionary for most traded with by roster id (roster_id, roster_id they have traded with the most).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> TradeCountsByRosterId => _tradeCountsByRosterId;

    /// <summary>
    /// Lookup dictionary for most losses against a roster_id (roster_id, roster_id they lost to the most).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> MostLossesAgainstRosterId => _mostLossesAgainstRosterId;

    /// <summary>
    /// Lookup dictionary for most losses against a roster_id (roster_id, roster_id they lost to the most).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyDictionary<int, int> MostWinsAgainstRosterId => _mostWinsAgainstRosterId;

    /// <summary>
    /// Lookup list of season (key) and Dictionary(rank, roster_id).
    /// Verify using EnsureLookupsLoadedAsync() before accessing.
    /// </summary>
    public IReadOnlyList<(string,Dictionary<int,int>)> EndOfSeasonFinishes => _endOfSeasonFinishes;


    /// <summary>
    /// Ensures the lookups are loaded
    /// </summary>
    private bool IsLookupLoaded => _lookupsLoaded;
    


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

        Matchups = _matchupState.AllMatchups?.ToList() ?? new List<MatchupModel>();
        Trades = _transactionState.GetFilteredTransactionsData(["trade"]);
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
            BuildMostTradedWithRosterId();
            BuildTradeCountsByRosterId();
            BuildMostWinsLossesAgainstRosterId();
            BuildEndOfSeasonFinishes();
            _lookupsLoaded = true;
        }
        catch (Exception ex)
        {
            _lookupTask = null;
            _lookupsLoaded = false;
            _logger.LogError(ex, "ERROR: {Message}", ex.Message);
            throw;
        }
    }


    /// <summary>
    /// Sets the top 10 highest scoring teams
    /// </summary>
    private void BuildTopScoringTeams()
    {
        _topScoringTeams.Clear();
        _topScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderByDescending(m => m.Points)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 lowest scoring teams
    /// </summary>
    private void BuildLowScoringTeams()
    {
        _lowScoringTeams.Clear();
        _lowScoringTeams = (Matchups ?? Enumerable.Empty<MatchupModel>())
                        .OrderBy(m => m.Points)
                        .ToList();
    }


    /// <summary>
    /// Sets the top 10 highest scoring players
    /// </summary>
    private void BuildTopScoringPlayers()
    {
        _topScoringPlayers.Clear();
        _topScoringPlayers = (Matchups ?? Enumerable.Empty<MatchupModel>())
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
        _lowScoringPlayers.Clear();
        _lowScoringPlayers = (Matchups ?? Enumerable.Empty<MatchupModel>())
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
        _highScoringWeeksByRosterId.Clear();
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
            if (!_highScoringWeeksByRosterId.TryGetValue(season, out var rosterCounts))
            {
                rosterCounts = new Dictionary<int, int>();
                _highScoringWeeksByRosterId[season] = rosterCounts;
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
                if (!_highScoringWeeksByRosterId.TryGetValue(group.Key.Season, out var rosterCounts))
                {
                    rosterCounts = new Dictionary<int, int>();
                    _highScoringWeeksByRosterId[group.Key.Season] = rosterCounts;
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
        _lowScoringWeeksByRosterId.Clear();
        var playoffStartByLeagueId = _leagueState.AllLeagues
            .Where(l => l.LeagueId is not null && l.Settings?.PlayoffWeekStart is int)
            .ToDictionary(l => l.LeagueId!, l => l.Settings!.PlayoffWeekStart);

        var allRosterIds = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Select(m => m.RosterId)
            .Distinct();

        foreach (var rosterId in allRosterIds)
        {
            _lowScoringWeeksByRosterId[rosterId] = 0;
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
                if (_lowScoringWeeksByRosterId.TryGetValue(loser.RosterId, out var count))
                {
                    _lowScoringWeeksByRosterId[loser.RosterId] = count + 1;
                }
                else
                {
                    _lowScoringWeeksByRosterId[loser.RosterId] = 1;
                }
            }
        }
    }


    /// <summary>
    /// Calculates and sets the top 10 largest margins of victory.
    /// </summary>
    private void BuildLargestMarginOfVictory()
    {
        _largestMarginOfVictory.Clear();
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

        _largestMarginOfVictory = winnersWithMargin
            .OrderByDescending(x => x.Value)
            .ToList();
    }


    /// <summary>
    /// Calculates and sets the top 10 tightest margins of victory.
    /// </summary>
    private void BuildTightestMarginOfVictory()
    {
        _tightestMarginOfVictory.Clear();
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

        _tightestMarginOfVictory = winnersWithMargin
            .OrderBy(x => x.Value)
            .ToList();
    }


    /// <summary>
    /// Builds a lookup of records by season/ week / rosterid 
    /// </summary>
    private void BuildRecordsBySeason()
    {
        _recordsBySeason.Clear();
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

            _recordsBySeason[season] = seasonRecord;
        }
    }


    /// <summary>
    /// Builds a lookup of a count of championships and finals appearances per roster_id.
    /// </summary>
    private void BuildChampionshipCountersByRosterId()
    {
        _championshipCountByRosterId.Clear();
        _finalsAppearancesByRosterId.Clear();

        _championshipCountByRosterId = new ()
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
        _finalsAppearancesByRosterId = new()
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
                _finalsAppearancesByRosterId[winnerId] = _finalsAppearancesByRosterId.TryGetValue(winnerId, out var wFinals)
                    ? wFinals + 1
                    : 1;

                _finalsAppearancesByRosterId[loserId] = _finalsAppearancesByRosterId.TryGetValue(loserId, out var lFinals)
                    ? lFinals + 1
                    : 1;

                // Championship count: only winner
                _championshipCountByRosterId[winnerId] = _championshipCountByRosterId.TryGetValue(winnerId, out var champs)
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
        _playoffAppearancesByRosterId.Clear();

        _playoffAppearancesByRosterId = new ()
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
                _playoffAppearancesByRosterId[rosterId] = _playoffAppearancesByRosterId.TryGetValue(rosterId, out var count)
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
        _lastPlaceFinishesByRosterId.Clear();

        _lastPlaceFinishesByRosterId = new ()
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

        var records = _recordsBySeason ?? new Dictionary<string, SeasonRecord>();

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


                    _lastPlaceFinishesByRosterId[bottomEntry.Key] = _lastPlaceFinishesByRosterId.TryGetValue(bottomEntry.Key, out var count)
                    ? count + 1
                    : 1;
                }
            }
        }

    }


    /// <summary>
    /// Builds a lookup of rosters that have been traded with the most per roster_id.
    /// </summary>
    private void BuildMostTradedWithRosterId()
    {
        _mostTradedWithRosterId.Clear();
        var partnersByRoster = new Dictionary<int, Dictionary<int, int>>();
        var tradeCounts = new Dictionary<(int, int), int>();

        foreach (var transaction in Trades ?? new List<TransactionsModel>())
        {
            if (transaction.ConsenterIds is null || transaction.ConsenterIds.Count < 2) continue;

            var consenters = transaction.ConsenterIds
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            for (var i = 0; i < consenters.Count - 1; i++)
            {
                for (var j = i + 1; j < consenters.Count; j++)
                {
                    var pair = (consenters[i], consenters[j]);
                    tradeCounts[pair] = tradeCounts.TryGetValue(pair, out var count) ? count + 1 : 1;
                }
            }
        }

        foreach (var kv in tradeCounts)
        {
            var a = kv.Key.Item1;
            var b = kv.Key.Item2;
            var count = kv.Value;

            if (!partnersByRoster.TryGetValue(a, out var aPartners))
                partnersByRoster[a] = aPartners = new Dictionary<int, int>();
            if (!partnersByRoster.TryGetValue(b, out var bPartners))
                partnersByRoster[b] = bPartners = new Dictionary<int, int>();

            aPartners[b] = count;
            bPartners[a] = count;
        }

        foreach (var roster in partnersByRoster)
        {
            var bestPartner = roster.Value
                .OrderByDescending(p => p.Value)
                .First().Key;

            _mostTradedWithRosterId[roster.Key] = bestPartner;
        }


    }


    /// <summary>
    /// Builds a lookup of count of trades per roster_id.
    /// </summary>
    private void BuildTradeCountsByRosterId()
    {
        _tradeCountsByRosterId.Clear();
        var tradeCounts = new Dictionary<int, int>();

        foreach (var transaction in Trades ?? new List<TransactionsModel>())
        {
            if (transaction.ConsenterIds is null) continue;

            foreach (var consenter in transaction.ConsenterIds)
            {
                tradeCounts[consenter] = tradeCounts.TryGetValue(consenter, out var count) ? count + 1 : 1;
            }
        }

        foreach (var kv in tradeCounts)
        {
            _tradeCountsByRosterId[kv.Key] = kv.Value;
        }
    }

    
    /// <summary>
    /// Builds a lookup of most losses against a roster_id.
    /// </summary>
    private void BuildMostWinsLossesAgainstRosterId()
    {
        _mostLossesAgainstRosterId.Clear();
        _mostWinsAgainstRosterId.Clear();

        var lossesByRoster = new Dictionary<int, Dictionary<int, int>>();
        var winsByRoster = new Dictionary<int, Dictionary<int, int>>();

        var groupedMatchups = (Matchups ?? Enumerable.Empty<MatchupModel>())
            .Where(m => m.MatchupId is not null)
            .GroupBy(m => (m.LeagueId, m.Season, m.Week, m.MatchupId));

        foreach (var group in groupedMatchups)
        {
            var teams = group.ToList();
            if (teams.Count < 2) continue;

            var winnerId = GetWinnerRosterId(teams);
            if (!winnerId.HasValue) continue;

            foreach (var team in teams)
            {
                if (team.RosterId == winnerId.Value) continue;

                if (!lossesByRoster.TryGetValue(team.RosterId, out var opponentCounts))
                {
                    opponentCounts = new Dictionary<int, int>();
                    lossesByRoster[team.RosterId] = opponentCounts;
                }

                opponentCounts[winnerId.Value] = opponentCounts.TryGetValue(winnerId.Value, out var count)
                    ? count + 1
                    : 1;
            }

            var winnerOpponents = teams.Where(t => t.RosterId != winnerId.Value).ToList();
            if (winnerOpponents.Count > 0)
            {
                if (!winsByRoster.TryGetValue(winnerId.Value, out var opponentCounts))
                {
                    opponentCounts = new Dictionary<int, int>();
                    winsByRoster[winnerId.Value] = opponentCounts;
                }

                foreach (var loser in winnerOpponents)
                {
                    opponentCounts[loser.RosterId] = opponentCounts.TryGetValue(loser.RosterId, out var count)
                        ? count + 1
                        : 1;
                }
            }
        }

        foreach (var roster in lossesByRoster)
        {
            var mostLossesOpponent = roster.Value
                .OrderByDescending(kv => kv.Value)
                .First().Key;

            _mostLossesAgainstRosterId[roster.Key] = mostLossesOpponent;
        }

        foreach (var roster in winsByRoster)
        {
            var mostWinsOpponent = roster.Value
                .OrderByDescending(kv => kv.Value)
                .First().Key;

            _mostWinsAgainstRosterId[roster.Key] = mostWinsOpponent;
        }
    }

    private void BuildEndOfSeasonFinishes()
    {
        _endOfSeasonFinishes.Clear();

        var playoffStartBySeason = _leagueState.AllLeagues
            .Where(l => !string.IsNullOrWhiteSpace(l.Season) && l.Settings?.PlayoffWeekStart is int)
            .GroupBy(l => l.Season!)
            .ToDictionary(g => g.Key, g => g.First().Settings!.PlayoffWeekStart);

        
        foreach (var seasonEntry in RecordsBySeason)
        {
            var season = seasonEntry.Key;
            var seasonRecord = seasonEntry.Value;

            if (!playoffStartBySeason.TryGetValue(season, out var playoffStart) || playoffStart <= 0)
            {
                continue;
            }

            var latestWeek = seasonRecord.WeeksByNumber.Keys
                .Where(w => w < playoffStart)
                .DefaultIfEmpty(0)
                .Max();

            if (latestWeek == 0 || !seasonRecord.WeeksByNumber.TryGetValue(latestWeek, out var weekRecord))
            {
                continue;
            }

            var ordered = weekRecord.ByRoster
                .Select(kv =>
                {
                    var pointsFor = double.TryParse(
                        kv.Value.PointsFor,
                        out var pf)
                        ? pf
                        : double.MinValue;

                    return new
                    {
                        RosterId = kv.Key,
                        Record = kv.Value,
                        PointsFor = pointsFor
                    };
                })
                .OrderByDescending(x => x.Record.Wins)
                .ThenByDescending(x => x.PointsFor)
                .ToList();

            var rankByRoster = new Dictionary<int, int>();
            var rank = 1;

            foreach (var entry in ordered)
            {
                rankByRoster[rank] = entry.RosterId;
                rank++;
            }

            _endOfSeasonFinishes.Add((season, rankByRoster));
        }
    }

    /// <summary>
    /// Sums the amount of high scoring weeks per roster_id.
    /// </summary>
    /// <param name="bySeason">IReadOnlyDictionary<season, Dictionary<roster_id, total_high_scoring_weeks>></param>
    /// <returns>Dictionary(roster_id,count)</returns>
    public Dictionary<int, int> SumHighScoringWeeks(Dictionary<string, Dictionary<int, int>> weeks_by_season)
    {
        var totals = new Dictionary<int, int>();
        foreach (var season in weeks_by_season.Values)
        {
            foreach (var kv in season)
            {
                if (totals.TryGetValue(kv.Key, out var count))
                {
                    totals[kv.Key] = count + kv.Value;
                }
                else
                {
                    totals[kv.Key] = kv.Value;
                }
            }
        }
        return totals;
    }

    /// <summary>
    /// Sums the records for each roster_id.
    /// </summary>
    /// <param name="season_records">Dictionary<season, SeasonRecord>. See StatsModel for more information on SeasonRecord.</param>
    /// <returns>Dictionary<roster_id, "W-L-T"></returns>
    public Dictionary<int, string> SumRecords(Dictionary<string, SeasonRecord> season_records)
    {
        var totals = new Dictionary<int, Record>();

        var playoffStartBySeason = _leagueState.AllLeagues
            .Where(l => !string.IsNullOrWhiteSpace(l.Season) && l.Settings?.PlayoffWeekStart is int)
            .GroupBy(l => l.Season!)
            .ToDictionary(g => g.Key, g => g.First().Settings!.PlayoffWeekStart);

        foreach (var seasonEntry in season_records)
        {
            var season = seasonEntry.Key;
            var seasonRecord = seasonEntry.Value;

            if (!playoffStartBySeason.TryGetValue(season, out var playoffStart))
            {
                continue;
            }

            var cutoffWeek = playoffStart > 0 ? playoffStart - 1 : int.MaxValue;
            var latestWeek = seasonRecord.WeeksByNumber.Keys
                .Where(w => w <= cutoffWeek)
                .DefaultIfEmpty(0)
                .Max();

            if (latestWeek == 0) continue;

            if (!seasonRecord.WeeksByNumber.TryGetValue(latestWeek, out var weekRecord)) continue;

            foreach (var kv in weekRecord.ByRoster)
            {
                if (!totals.TryGetValue(kv.Key, out var record))
                {
                    record = new Record();
                    totals[kv.Key] = record;
                }

                record.Wins += kv.Value.Wins;
                record.Losses += kv.Value.Losses;
                record.Ties += kv.Value.Ties;
            }
        }

        var result = new Dictionary<int, string>();
        foreach (var kv in totals)
        {
            result[kv.Key] = kv.Value.Display;
        }

        return result;
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
        if (IsLookupLoaded) return Task.CompletedTask;
        _lookupTask ??= BuildLookupsAsync();
        return _lookupTask;
    }
}